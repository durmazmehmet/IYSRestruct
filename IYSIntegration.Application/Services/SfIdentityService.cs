using IYSIntegration.Application.Interface;
using IYSIntegration.Common.Request.Identity;
using IYSIntegration.Common.Response.Identity;
using IYSIntegration.Common.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;

namespace IYSIntegration.Application.Services
{
    public class SfIdentityService : ISfIdentityService
    {
        private SfToken token;
        private readonly IConfiguration _config;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly ICacheService _cacheService;
        private readonly ILogger<SfIdentityService> _logger;

        public SfIdentityService(IConfiguration config, ICacheService cacheService, ILogger<SfIdentityService> logger)
        {
            _config = config;
            _cacheService = cacheService;
            _logger = logger;
        }

        private SfCredential GetSfCredential()
        {
            return new SfCredential
            {
                Username = _config.GetValue<string>($"Salesforce:Username"),
                Password = _config.GetValue<string>($"Salesforce:Password"),
                GrantType = _config.GetValue<string>($"Salesforce:GrantType"),
                ClientId = _config.GetValue<string>($"Salesforce:ClientId"),
                ClientSecret = _config.GetValue<string>($"Salesforce:ClientSecret")
            };
        }

        private async Task<SfToken> GetNewToken()
        {
            var client = new RestClient(_config.GetValue<string>($"Salesforce:BaseUrl"));
            var request = new RestRequest("oauth2/token", Method.Post);
            var credentail = GetSfCredential();

            //request.AddObject(credentail);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("username", credentail.Username, ParameterType.GetOrPost);
            request.AddParameter("password", credentail.Password, ParameterType.GetOrPost);
            request.AddParameter("grant_type", credentail.GrantType, ParameterType.GetOrPost);
            request.AddParameter("client_id", credentail.ClientId, ParameterType.GetOrPost);
            request.AddParameter("client_secret", credentail.ClientSecret, ParameterType.GetOrPost);

            RestResponse response = await client.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                return JsonConvert.DeserializeObject<SfToken>(response.Content);
            }
            else
            {
                // TODO: Loglama ekelenecek, alert edilebilir
            }

            return null;
        }

        public async Task<SfToken> GetToken(bool isReset)
        {
            await _semaphore.WaitAsync();
            try
            {
                var token = isReset ? null : await _cacheService.GetCachedHashDataAsync<SfToken>("IYS_Token", "SF");

                if (token == null)
                {
                    token = await GetNewToken() ?? throw new Exception("SF Token alınamadı");
                }

                await _cacheService.SetCacheHashDataAsync("IYS_Token", "SF", token);

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
}
