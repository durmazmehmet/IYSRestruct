using IYS.Application.Services.Interface;
using IYS.Application.Services.Models.Response.Identity;
using IYS.Application.Services.Models.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System.Globalization;

namespace IYS.Application.Services
{
    public class IysIdentityService : IIysIdentityService
    {
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IIysHelper _iysHelper;
        private readonly ILogger<IysIdentityService> _logger;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly string _serverIdentifier;
        private const int TokenRefreshBufferSeconds = 300;

        public IysIdentityService(
            IConfiguration config,
            ILogger<IysIdentityService> logger,
            ICacheService cacheService,
            IIysHelper iysHelper,
            IServiceScopeFactory scopeFactory)
        {
            _config = config;
            _logger = logger;
            _cacheService = cacheService;
            _scopeFactory = scopeFactory;
            _serverIdentifier = ResolveServerIdentifier();
            _iysHelper = iysHelper;
        }

        private async Task<Token> GetNewToken(int iysCode)
        {
            var credential = new Credential
            {
                Username = _config.GetValue<string>($"{iysCode}:Username"),
                Password = _config.GetValue<string>($"{iysCode}:Password"),
                Granttype = "password"
            };

            var client = new RestClient(_config.GetValue<string>("BaseUrl"));
            var request = new RestRequest("oauth2/token", Method.Post);
            request.AddHeader("content-type", "application/json");
            request.AddStringBody(JsonConvert.SerializeObject(credential), DataFormat.Json);

            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                _logger.LogError($"New Token Hatası: {response.Content}");
                throw new Exception($"New Token Hatası: {response.Content}");
            }

            var token = JsonConvert.DeserializeObject<Token>(response.Content);
            token.TokenValidTill = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
            token.RefreshTokenValidTill = DateTime.UtcNow.AddSeconds(token.RefreshExpiresIn);

            return token;
        }

        private async Task<Token?> RefreshToken(Token oldToken)
        {
            var client = new RestClient(_config.GetValue<string>("BaseUrl"));
            var request = new RestRequest("oauth2/token", Method.Post);
            request.AddHeader("content-type", "application/json");

            var body = new RefreshTokenRequest
            {
                RefreshToken = oldToken.RefreshToken,
                Granttype = "refresh_token"
            };

            request.AddStringBody(JsonConvert.SerializeObject(body), DataFormat.Json);

            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                _logger.LogError($"Refresh Token Hatası: {response.Content}");
                return null;
            }

            var token = JsonConvert.DeserializeObject<Token>(response.Content);
            token.TokenValidTill = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
            token.RefreshTokenValidTill = DateTime.UtcNow.AddSeconds(token.RefreshExpiresIn);

            return token;
        }

        public async Task<Token> GetToken(int iysCode, bool isReset)
        {
            await _semaphore.WaitAsync();
            try
            {
                var token = isReset
                    ? null
                    : await _cacheService.GetCachedHashDataAsync<Token>("IYS_Token", iysCode.ToString());

                if (string.IsNullOrEmpty(token?.AccessToken) || token?.RefreshTokenValidTill <= DateTime.UtcNow)
                {
                    // refresh_token süresi bitmiş → sıfırdan al
                    token = await GetNewToken(iysCode) ?? throw new Exception("Token alınamadı");
                    await _cacheService.SetCacheHashDataAsync("IYS_Token", iysCode.ToString(), token);
                    await LogTokenLifecycleAsync(iysCode, token, "New");
                }
                else if (token?.TokenValidTill <= DateTime.UtcNow.AddSeconds(TokenRefreshBufferSeconds))
                {
                    // access_token süresi 5 dakika içinde dolacak → refresh dene
                    var refreshedToken = await RefreshToken(token);

                    if (refreshedToken is not null)
                    {
                        token = refreshedToken;
                        await _cacheService.SetCacheHashDataAsync("IYS_Token", iysCode.ToString(), token);
                        await LogTokenLifecycleAsync(iysCode, token, "Refresh");
                    }
                    else
                    {
                        // refresh başarısız → yeniden sıfırdan al
                        token = await GetNewToken(iysCode) ?? throw new Exception("Token alınamadı");
                        await _cacheService.SetCacheHashDataAsync("IYS_Token", iysCode.ToString(), token);
                        await LogTokenLifecycleAsync(iysCode, token, "New");
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
            if (token is null) return;

            string? companyCode = null;

            try
            {
                companyCode = _iysHelper.GetCompanyCode(iysCode) ?? iysCode.ToString(CultureInfo.InvariantCulture);

                var logEntry = new TokenLogEntry
                {
                    CompanyCode = companyCode,
                    AccessTokenMasked = MaskToken(token.AccessToken),
                    RefreshTokenMasked = MaskToken(token.RefreshToken),
                    TokenUpdateDateUtc = DateTime.UtcNow,
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

        private static string? MaskToken(string? token)
        {
            var TokenMaskSegmentLength = 4;

            if (string.IsNullOrWhiteSpace(token)) return null;

            var trimmed = token.Trim();
            if (trimmed.Length <= TokenMaskSegmentLength * 2) return trimmed;

            var firstPart = trimmed.Substring(0, TokenMaskSegmentLength);
            var lastPart = trimmed.Substring(trimmed.Length - TokenMaskSegmentLength, TokenMaskSegmentLength);

            return string.Concat(firstPart, "***", lastPart);
        }

        private string ResolveServerIdentifier()
        {
            var configuredName = _config.GetValue<string>("ServerIdentifier");
            if (!string.IsNullOrWhiteSpace(configuredName)) return configuredName.Trim();

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
}
