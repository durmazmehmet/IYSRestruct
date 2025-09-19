using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Identity;
using IYSIntegration.Application.Services.Models.Response.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Globalization;
using System.Threading;

namespace IYSIntegration.Application.Services;

public class IysIdentityService : IIysIdentityService
{
    private readonly ICacheService _cacheService;
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly int TokenExpiry;
    private readonly int RefreshTokenExpiry;
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly ILogger<IysIdentityService> _logger;
    private readonly string _serverIdentifier;
    private const int TokenMaskSegmentLength = 4;
    private const string TokenMaskSeparator = ".....";
    private const string OperationNew = "NEW";
    private const string OperationRefresh = "REFRESH";

    public IysIdentityService(
        IConfiguration config,
        ILogger<IysIdentityService> logger,
        ICacheService cacheService,
        IServiceScopeFactory scopeFactory)
    {
        _config = config;
        _logger = logger;
        _cacheService = cacheService;
        _scopeFactory = scopeFactory;
        TokenExpiry = _config.GetValue<int>($"TokenExpiry", 7000);
        RefreshTokenExpiry = _config.GetValue<int>($"RefreshTokenExpiry", 14000);
        _serverIdentifier = ResolveServerIdentifier();
    }

    private async Task<Token> GetNewToken(int iysCode, DateTime previousDate)
    {

        var IYSCredential = new Credential
        {
            Username = _config.GetValue<string>($"{iysCode}:Username"),
            Password = _config.GetValue<string>($"{iysCode}:Password"),
            Granttype = "password"
        };
        var client = new RestClient(_config.GetValue<string>($"BaseUrl"));
        var request = new RestRequest("oauth2/token", Method.Post);
        request.AddHeader("content-type", "application/json");
        request.AddParameter("application/json", JsonConvert.SerializeObject(IYSCredential), ParameterType.RequestBody);
        RestResponse response = await client.ExecuteAsync(request);

        if (response.IsSuccessful)
        {
            var token = JsonConvert.DeserializeObject<Token>(response.Content);
            token.TokenValidTill = DateTime.UtcNow.AddSeconds(TokenExpiry);
            token.RefreshTokenValidTill = DateTime.UtcNow.AddSeconds(RefreshTokenExpiry);
            token.CreateDate = DateTime.UtcNow;
            token.PreviousDate = previousDate;
            return token;
        }
        else
        {
            _logger.LogError($"New Token Hatası: {response.Content}");
            throw new Exception($"New Token Hatası: {response.Content}");
        }
    }

    private async Task<Token> RefreshToken(Token token, DateTime previousDate)
    {

        var client = new RestClient(_config.GetValue<string>($"BaseUrl"));
        var httpRequest = new RestRequest("oauth2/token", Method.Post);
        httpRequest.AddHeader("content-type", "application/json");
        var request = new RefreshTokenRequest { RefreshToken = token.RefreshToken, Granttype = "refresh_token" };
        httpRequest.AddParameter("application/json", JsonConvert.SerializeObject(request), ParameterType.RequestBody);
        RestResponse response = await client.ExecuteAsync(httpRequest);

        if (response.IsSuccessful)
        {
            var refreshToken = JsonConvert.DeserializeObject<Token>(response.Content);
            refreshToken.TokenValidTill = DateTime.UtcNow.AddSeconds(TokenExpiry);
            refreshToken.RefreshTokenValidTill = DateTime.UtcNow.AddSeconds(RefreshTokenExpiry);
            refreshToken.CreateDate = token.CreateDate == default ? DateTime.UtcNow : token.CreateDate;
            refreshToken.RefreshDate = DateTime.UtcNow;
            refreshToken.PreviousDate = previousDate;
            return refreshToken;
        }
        else
        {
            _logger.LogError($"Refresh Token Hatası: {response.Content}");
        }

        return null;
    }

    public async Task<Token> GetToken(int iysCode, bool isReset)
    {
        await _semaphore.WaitAsync();
        try
        {
            var token = isReset ? null : await _cacheService.GetCachedHashDataAsync<Token>("IYS_Token", iysCode.ToString());

            if (string.IsNullOrEmpty(token?.AccessToken ?? null) || token?.RefreshTokenValidTill < DateTime.UtcNow)
            {
                var previouseValidDate = token?.RefreshTokenValidTill ?? DateTime.MinValue;
                token = await GetNewToken(iysCode, previouseValidDate) ?? throw new Exception("Token alınamadı");
                await _cacheService.SetCacheHashDataAsync("IYS_Token", iysCode.ToString(), token);
                await LogTokenLifecycleAsync(iysCode, token, OperationNew);

            }
            else if (token?.TokenValidTill < DateTime.UtcNow)
            {
                var previouseValidDate = token?.TokenValidTill ?? DateTime.MinValue;
                var refreshedToken = await RefreshToken(token, previouseValidDate);

                if (refreshedToken is not null)
                {
                    token = refreshedToken;
                    await _cacheService.SetCacheHashDataAsync("IYS_Token", iysCode.ToString(), token);
                    await LogTokenLifecycleAsync(iysCode, token, OperationRefresh);
                }
                else
                {
                    token = await GetNewToken(iysCode, previouseValidDate) ?? throw new Exception("Token alınamadı");
                    await _cacheService.SetCacheHashDataAsync("IYS_Token", iysCode.ToString(), token);
                    await LogTokenLifecycleAsync(iysCode, token, OperationNew);
                }
            }

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Token Hatası: {ex.Message}");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task LogTokenLifecycleAsync(int iysCode, Token token, string operation)
    {
        if (token is null)
        {
            return;
        }

        string? companyCode = null;

        try
        {
            companyCode = ResolveCompanyCode(iysCode);

            if (string.IsNullOrWhiteSpace(companyCode))
            {
                companyCode = iysCode.ToString(CultureInfo.InvariantCulture);
            }

            var tokenCreateDate = token.CreateDate != default
                ? token.CreateDate
                : token.RefreshDate != default
                    ? token.RefreshDate
                    : DateTime.UtcNow;

            var logEntry = new TokenLogEntry
            {
                CompanyCode = companyCode,
                AccessTokenMasked = MaskToken(token.AccessToken),
                RefreshTokenMasked = MaskToken(token.RefreshToken),
                TokenCreateDateUtc = tokenCreateDate,
                TokenRefreshDateUtc = token.RefreshDate != default ? token.RefreshDate : (DateTime?)null,
                Operation = operation,
                ServerIdentifier = _serverIdentifier
            };

            using var scope = _scopeFactory.CreateScope();
            var dbService = scope.ServiceProvider.GetRequiredService<IDbService>();
            await dbService.InsertTokenLogAsync(logEntry);
        }
        catch (Exception ex)
        {
                _logger.LogError(
                    ex,
                    "Token loglaması sırasında hata oluştu (CompanyCode: {CompanyCode}, IysCode: {IysCode}, Operation: {Operation}, Server: {Server}).",
                    companyCode ?? string.Empty,
                    iysCode,
                    operation,
                    _serverIdentifier);
        }
    }

    private string? ResolveCompanyCode(int iysCode)
    {
        foreach (var section in _config.GetChildren())
        {
            if (string.Equals(section.Key, "ConnectionStrings", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var iysValue = _config.GetValue<int?>($"{section.Key}:IysCode");
            var brandValue = _config.GetValue<int?>($"{section.Key}:BrandCode");

            if (iysValue == iysCode || brandValue == iysCode)
            {
                return NormalizeCompanyCode(section.Key);
            }
        }

        return null;
    }

    private static string NormalizeCompanyCode(string companyCode)
    {
        if (string.IsNullOrWhiteSpace(companyCode))
        {
            return string.Empty;
        }

        var trimmed = companyCode.Trim();

        if (string.Equals(trimmed, "BAI", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "BOD", StringComparison.OrdinalIgnoreCase))
        {
            return "BOD";
        }

        return trimmed;
    }

    private static string? MaskToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var trimmed = token.Trim();

        if (trimmed.Length <= TokenMaskSegmentLength * 2)
        {
            return trimmed;
        }

        var firstPart = trimmed.Substring(0, TokenMaskSegmentLength);
        var lastPart = trimmed.Substring(trimmed.Length - TokenMaskSegmentLength, TokenMaskSegmentLength);

        return string.Concat(firstPart, TokenMaskSeparator, lastPart);
    }

    private string ResolveServerIdentifier()
    {
        var configuredName = _config.GetValue<string>("ServerIdentifier");

        if (!string.IsNullOrWhiteSpace(configuredName))
        {
            return configuredName.Trim();
        }

        try
        {
            var machineName = Environment.MachineName;
            return string.IsNullOrWhiteSpace(machineName) ? "UNKNOWN" : machineName;
        }
        catch
        {
            return "UNKNOWN";
        }
    }
}
