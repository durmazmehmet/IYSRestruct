using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Request.Identity;
using IYSIntegration.Application.Services.Models.Response.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;

namespace IYSIntegration.Application.Services;

public class IysIdentityService : IIysIdentityService
{
    private readonly ICacheService _cacheService;
    private readonly IConfiguration _config;
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    ILogger<IysIdentityService> _logger;

    public IysIdentityService(IConfiguration config, ILogger<IysIdentityService> logger, ICacheService cacheService)
    {
        _config = config;
        _logger = logger;
        _cacheService = cacheService;
    }

    private async Task<Token> GetNewToken(int iysCode)
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
            token.TokenValidTill = DateTime.UtcNow.AddMinutes(110);
            token.RefreshTokenValidTill = DateTime.UtcNow.AddMinutes(230);
            return token;
        }
        else
        {
            _logger.LogError($"New Token Hatası: {response.Content}");
            throw new Exception($"New Token Hatası: {response.Content}");
        }
    }

    private async Task<Token> RefreshToken(Token token)
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
            refreshToken.TokenValidTill = DateTime.UtcNow.AddMinutes(110);
            refreshToken.RefreshTokenValidTill = DateTime.UtcNow.AddMinutes(230);
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
                token = await GetNewToken(iysCode) ?? throw new Exception("Token alınamadı");
            }
            else if (token.TokenValidTill < DateTime.UtcNow)
            {
                token = await RefreshToken(token) ?? await GetNewToken(iysCode);
            }

            await _cacheService.SetCacheHashDataAsync("IYS_Token", iysCode.ToString(), token);

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
}
