using IYS.Application.Services.Exceptions;
using IYS.Application.Services.Interface;
using IYS.Application.Services.Models.Identity;
using IYS.Application.Services.Models.Response.Identity;
using Microsoft.Extensions.Configuration;
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
        private readonly IIysHelper _iysHelper;
        private readonly IDbService _dbService;
        private readonly ILogger<IysIdentityService> _logger;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly string _serverIdentifier;
        private const int TokenRefreshBufferSeconds = 300;
        private const string TokenCacheHashKey = "IYS_Token";
        private const string RateLimitErrorCode = "H084";
        private static readonly TimeSpan RateLimitDuration = TimeSpan.FromHours(1);

        public IysIdentityService(
            IConfiguration config,
            ILogger<IysIdentityService> logger,
            ICacheService cacheService,
            IIysHelper iysHelper,
            IDbService dbService)
        {
            _config = config;
            _logger = logger;
            _cacheService = cacheService;
            _dbService = dbService;
            _iysHelper = iysHelper;
        }

        private async Task<Token> GetNewToken(int iysCode)
        {
            var credential = new Credential
            {
                Username = _config.GetValue<string>($"{iysCode}:Username"),
                Password = _config.GetValue<string>($"{iysCode}:Password"),
                GrantType = "password"
            };

            var client = new RestClient(_config.GetValue<string>("BaseUrl"));
            var request = new RestRequest("oauth2/token", Method.Post);
            request.AddHeader("content-type", "application/json");
            request.AddStringBody(JsonConvert.SerializeObject(credential), DataFormat.Json);

            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                var identityError = ParseIdentityError(response.Content);

                if (string.Equals(identityError?.Code, RateLimitErrorCode, StringComparison.OrdinalIgnoreCase))
                {
                    await HandleRateLimitAsync(iysCode, identityError, response.Content);
                }

                _logger.LogError($"New Token Hatası: {response.Content}");
                throw new Exception($"New Token Hatası: {response.Content}");
            }

            var token = JsonConvert.DeserializeObject<Token>(response.Content);
            token.TokenValidTill = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
            token.RefreshTokenValidTill = DateTime.UtcNow.AddSeconds(token.RefreshExpiresIn);

            return token;
        }

        private async Task<Token?> RefreshToken(int iysCode, Token oldToken)
        {
            var client = new RestClient(_config.GetValue<string>("BaseUrl"));
            var request = new RestRequest("oauth2/token", Method.Post);
            request.AddHeader("content-type", "application/json");

            var body = new RefreshTokenRequest
            {
                RefreshToken = oldToken.RefreshToken,
                GrantType = "refresh_token"
            };

            request.AddStringBody(JsonConvert.SerializeObject(body), DataFormat.Json);

            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                var identityError = ParseIdentityError(response.Content);

                if (string.Equals(identityError?.Code, RateLimitErrorCode, StringComparison.OrdinalIgnoreCase))
                {
                    await HandleRateLimitAsync(iysCode, identityError, response.Content);
                }

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
                await EnsureRateLimitNotActiveAsync(iysCode);

                var iysCodeKey = iysCode.ToString(CultureInfo.InvariantCulture);

                var token = isReset
                    ? null
                    : await _cacheService.GetCachedHashDataAsync<Token>(TokenCacheHashKey, iysCodeKey);

                if (string.IsNullOrEmpty(token?.AccessToken) || token?.RefreshTokenValidTill <= DateTime.UtcNow)
                {
                    // refresh_token süresi bitmiş → sıfırdan al
                    token = await GetNewToken(iysCode) ?? throw new Exception("Token alınamadı");
                    await _cacheService.SetCacheHashDataAsync(TokenCacheHashKey, iysCodeKey, token);
                    await LogTokenLifecycleAsync(iysCode, token, "New");
                }
                else if (token?.TokenValidTill <= DateTime.UtcNow.AddSeconds(TokenRefreshBufferSeconds))
                {
                    // access_token süresi 5 dakika içinde dolacak → refresh dene
                    var refreshedToken = await RefreshToken(iysCode, token);

                    if (refreshedToken is not null)
                    {
                        token = refreshedToken;
                        await _cacheService.SetCacheHashDataAsync(TokenCacheHashKey, iysCodeKey, token);
                        await LogTokenLifecycleAsync(iysCode, token, "Refresh");
                    }
                    else
                    {
                        // refresh başarısız → yeniden sıfırdan al
                        token = await GetNewToken(iysCode) ?? throw new Exception("Token alınamadı");
                        await _cacheService.SetCacheHashDataAsync(TokenCacheHashKey, iysCodeKey, token);
                        await LogTokenLifecycleAsync(iysCode, token, "New");
                    }
                }

                return token;
            }
            catch (TokenRateLimitException)
            {
                throw;
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

                await _dbService.InsertTokenLogAsync(logEntry);
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

        private async Task EnsureRateLimitNotActiveAsync(int iysCode)
        {
            var cacheKey = ComposeTokenCacheKey(iysCode);
            var haltUntil = await _cacheService.GetCachedHaltUntilAsync(cacheKey);

            if (!haltUntil.HasValue)
            {
                haltUntil = await _dbService.GetTokenHaltUntilAsync(cacheKey);

                if (haltUntil.HasValue)
                {
                    await _cacheService.SetCachedHaltUntilAsync(cacheKey, haltUntil);
                }
            }

            if (haltUntil.HasValue)
            {
                if (haltUntil.Value > DateTime.UtcNow)
                {
                    var message = BuildRateLimitMessage(haltUntil.Value);
                    _logger.LogWarning(
                        "IYS {IysCode} için token alma işlemi {HaltUntil:o} UTC'ye kadar duraklatıldı.",
                        iysCode,
                        haltUntil.Value);

                    throw new TokenRateLimitException(RateLimitErrorCode, haltUntil, message);
                }

                await _dbService.SetTokenHaltUntilAsync(cacheKey, null);
                await _cacheService.SetCachedHaltUntilAsync(cacheKey, null);
            }
        }

        private async Task HandleRateLimitAsync(int iysCode, IdentityErrorResponse? error, string? rawContent)
        {
            var haltUntil = DateTime.UtcNow.Add(RateLimitDuration);
            var cacheKey = ComposeTokenCacheKey(iysCode);
            await _dbService.SetTokenHaltUntilAsync(cacheKey, haltUntil);
            await _cacheService.SetCachedHaltUntilAsync(cacheKey, haltUntil);

            var message = !string.IsNullOrWhiteSpace(error?.Message)
                ? error!.Message!
                : BuildRateLimitMessage(haltUntil);

            _logger.LogWarning(
                "IYS {IysCode} için yeni token alınamadı. İstek limiti aşıldı ve {HaltUntil:o} UTC'ye kadar bekleniyor. Hata: {ErrorContent}",
                iysCode,
                haltUntil,
                string.IsNullOrWhiteSpace(rawContent) ? error?.Message : rawContent);

            throw new TokenRateLimitException(RateLimitErrorCode, haltUntil, message);
        }

        private static string ComposeTokenCacheKey(int iysCode)
        {
            var key = iysCode.ToString(CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(key)
                ? TokenCacheHashKey
                : $"{TokenCacheHashKey}:{key}";
        }

        private static string BuildRateLimitMessage(DateTime haltUntilUtc)
            => $"Saatte kabul edilen kimlik doğrulama istek limitine ulaşıldı. İşlemler {haltUntilUtc:O} UTC'ye kadar durduruldu.";

        private static IdentityErrorResponse? ParseIdentityError(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<IdentityErrorResponse>(content);
            }
            catch
            {
                return null;
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

        private sealed class IdentityErrorResponse
        {
            [JsonProperty("code")]
            public string? Code { get; set; }

            [JsonProperty("message")]
            public string? Message { get; set; }
        }
    }
}
