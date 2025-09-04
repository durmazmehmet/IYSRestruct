using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Request.Consent;
using IYSIntegration.Application.Response.Consent;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
namespace IYSIntegration.Application.Services
{
    public class SfConsentService : ISfConsentService
    {
        private readonly IConfiguration _config;
        private readonly ISfIdentityService _identityManager;
        private readonly IDbService _dbService;
        public SfConsentService(IConfiguration config, ISfIdentityService identityManager, IDbService dbHelper)
        {
            _config = config;
            _identityManager = identityManager;
            _dbService = dbHelper;
        }

        public async Task<SfConsentAddResponse> AddConsent(SfConsentAddRequest request)
        {
            //Hotfix for sf error
            var properConsents = new List<Application.Base.Consent>();
            foreach (var consent in request.Request.Consents)
            {
                properConsents.Add(new Application.Base.Consent
                {
                    ConsentDate = consent.ConsentDate,
                    Source = consent.Source,
                    Recipient = consent.Recipient,
                    RecipientType = consent.RecipientType,
                    Status = consent.Status,
                    Type = consent.Type,
                });
            }
            var properRequest = new SfConsentAddRequest
            {
                Request = new SfConsentBase
                {
                    CompanyCode = request.Request.CompanyCode,
                    Consents = properConsents
                }
            };

            var SfRequest = new Application.Base.IysRequest<SfConsentAddRequest>
            {
                Url = _config.GetValue<string>($"Salesforce:BaseUrl") + "/apexrest/iys",
                Body = properRequest,
                Action = "Salesforce Add Consent"
            };

            var logId = await _dbService.InsertLog(SfRequest);
            // TODO: Geçici olarak eklendi, Sf tarafında token expiry date, refresh vs yapılırsa düzenlenecek
            var token = await _identityManager.GetToken(true);

            var client = new RestClient(new RestClientOptions(SfRequest.Url)
            {
                Authenticator = new JwtAuthenticator(token.AccessToken)
            });


            var httpRequest = new RestRequest();
            if (SfRequest.Body != null)
            {
                httpRequest.AddParameter("application/json", JsonConvert.SerializeObject(SfRequest.Body,
                                         Formatting.None,
                                         new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }),
                                         ParameterType.RequestBody);
            }

            var httpResponse = await client.PostAsync(httpRequest);
            await _dbService.UpdateLog(httpResponse, logId);

            if (httpResponse.IsSuccessful)
            {
                var result = JsonConvert.DeserializeObject<SfConsentAddResponse>(httpResponse.Content);
                result.LogId = logId;
                return result;
            }
            else
            {
                var errorResponse = JsonConvert.DeserializeObject<List<SfConsentAddErrorResponse>>(httpResponse.Content);
                return new SfConsentAddResponse
                {
                    LogId = logId,
                    WsStatus = "ERROR",
                    WsDescription = $"{errorResponse.FirstOrDefault().errorCode}-{errorResponse.FirstOrDefault().message}"
                };
            }
        }
    }
}
