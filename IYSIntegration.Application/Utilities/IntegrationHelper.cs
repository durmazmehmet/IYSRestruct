using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Common.Response.Consent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IYSIntegration.Application.Utilities
{
    public class IntegrationHelper : IIntegrationHelper
    {
        private readonly IConfiguration _config;
        private readonly ILogger<IntegrationHelper> _logger;
        public IntegrationHelper(IConfiguration config, ILogger<IntegrationHelper> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<ResponseBase<AddConsentResult>> AddConsent(AddConsentRequest request)
        {
            ResponseBase<AddConsentResult> result = new();

            string url = $"{_config.GetValue<string>($"BaseUrl")}";
            var client = new RestClient(url);
            var httpRequest = new RestRequest("addConsent", Method.Post);
            var req = JsonConvert.SerializeObject(request).ToString();
            httpRequest.AddParameter("application/json", JsonConvert.SerializeObject(request), ParameterType.RequestBody);
            var response = await client.ExecuteAsync(httpRequest);

            var h = response.IsSuccessful;

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                result = JsonConvert.DeserializeObject<ResponseBase<AddConsentResult>>(response.Content);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                _logger.LogWarning($"{request.CompanyCode} için AddConsentResult'den yanıt gelmedi");
            }
            else
            {
                throw new Exception($"{request.CompanyCode} için AddConsentResult hatası: {response.StatusCode} Message: {response.ErrorMessage}");
            }
            return result;
        }

        public async Task<ResponseBase<MultipleConsentResult>> SendMultipleConsent(MultipleConsentRequest request)
        {
            ResponseBase<MultipleConsentResult> result = new();

            string url = $"{_config.GetValue<string>($"BaseUrl")}";
            var client = new RestClient(url);
            var httpRequest = new RestRequest("sendMultipleConsent", Method.Post);
            httpRequest.AddParameter("application/json", JsonConvert.SerializeObject(request), ParameterType.RequestBody);
            var response = await client.ExecuteAsync(httpRequest);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                result = JsonConvert.DeserializeObject<ResponseBase<MultipleConsentResult>>(response.Content);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                _logger.LogWarning($"{request.CompanyCode} için MultipleConsentResult'den yanıt gelmedi");
            }
            else
            {
                throw new Exception($"{request.CompanyCode} için MultipleConsentResult hatası: {response.StatusCode} Message: {response.ErrorMessage}");
            }
            return result;
        }

        public async Task<ResponseBase<List<QueryMultipleConsentResult>>> QueryMultipleConsent(QueryMultipleConsentRequest request)
        {
            ResponseBase<List<QueryMultipleConsentResult>> result = new();

            string url = $"{_config.GetValue<string>($"BaseUrl")}";
            var client = new RestClient(url);
            var httpRequest = new RestRequest("queryMultipleConsent", Method.Post);
            httpRequest.AddParameter("application/json", JsonConvert.SerializeObject(request), ParameterType.RequestBody);
            var response = await client.ExecuteAsync(httpRequest);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                result = JsonConvert.DeserializeObject<ResponseBase<List<QueryMultipleConsentResult>>>(response.Content);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                _logger.LogWarning($"{request.CompanyCode} için QueryMultipleConsentResult'den yanıt gelmedi");
            }
            else
            {
                throw new Exception($"{request.CompanyCode} için QueryMultipleConsentResult hatası: {response.StatusCode} Message: {response.ErrorMessage}");
            }
            return result;
        }

        public async Task<ResponseBase<PullConsentResult>> PullConsent(PullConsentRequest request)
        {
            var result = new ResponseBase<PullConsentResult>();

            string url = $"{_config.GetValue<string>($"BaseUrl")}";
            var client = new RestClient(url);
            var httpRequest = new RestRequest("pullConsent", Method.Post);
            var req = JsonConvert.SerializeObject(request).ToString();
            httpRequest.AddParameter("application/json", JsonConvert.SerializeObject(request), ParameterType.RequestBody);
            var response = await client.ExecuteAsync(httpRequest);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                result = JsonConvert.DeserializeObject<ResponseBase<PullConsentResult>>(response.Content);
                _logger.LogInformation($"{request.CompanyCode} için PullConsent'den alındı");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                _logger.LogWarning($"{request.CompanyCode} için PullConsent'den yanıt gelmedi");
            }
            else
            {
                _logger.LogInformation($"{request.CompanyCode} için PullConsent'den hata alındı {response.StatusCode}");
            }
            return result;
        }

        public async Task<SfConsentAddResponse> SfAddConsent(SfConsentAddRequest request)
        {
            var result = new SfConsentAddResponse();

            string url = $"{_config.GetValue<string>($"BaseUrl")}";
            var client = new RestClient(url);
            var httpRequest = new RestRequest("sfaddconsent", Method.Post);
            httpRequest.AddParameter("application/json", JsonConvert.SerializeObject(request), ParameterType.RequestBody);
            var response = await client.ExecuteAsync(httpRequest);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                result = JsonConvert.DeserializeObject<SfConsentAddResponse>(response.Content);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                _logger.LogWarning($"{request.Request.CompanyCode} için SfAddConsent'den yanıt gelmedi");
            }
            else
            {
                throw new Exception($"{request.Request.CompanyCode} için SfAddConsent hatası: {response.StatusCode} Message: {response.ErrorMessage}");
            }

            return result;
        }
    }
}
