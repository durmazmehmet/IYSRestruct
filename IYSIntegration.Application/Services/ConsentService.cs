using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Base;
using IYSIntegration.Application.Error;
using IYSIntegration.Application.Request;
using IYSIntegration.Application.Request.Consent;
using IYSIntegration.Application.Response.Consent;
using IYSIntegration.Application.Services;
using Microsoft.Extensions.Configuration;

namespace IYSIntegration.Application.Services
{
    public class ConsentService : IConsentService
    {
        private readonly IConfiguration _config;
        private readonly IRestClientService _clientHelper;
        private readonly string _baseUrl;
        private readonly ICacheService _cacheService;

        public ConsentService(IConfiguration config, IRestClientService clientHelper, ICacheService cacheService)
        {
            _config = config;
            _clientHelper = clientHelper;
            _baseUrl = _config.GetValue<string>("BaseUrl");
            _cacheService = cacheService;
        }

        public async Task<ResponseBase<AddConsentResult>> AddConsent(AddConsentRequest request)
        {
            
            var iysRequest = new IysRequest<Consent>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/consents",
                Body = request.Consent,
                Action = "Add Consent",
                Method = RestSharp.Method.Post
            };

            return await _clientHelper.Execute<AddConsentResult, Consent>(iysRequest);
        }

        

        public async Task<ResponseBase<QueryConsentResult>> QueryConsent(QueryConsentRequest request)
        {
            var iysRequest = new IysRequest<RecipientKey>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/consents/status",
                Body = request.RecipientKey,
                Action = "Query Consent",
                Method = RestSharp.Method.Post
            };

            return await _clientHelper.Execute<QueryConsentResult, RecipientKey>(iysRequest);
        }

        public async Task<ResponseBase<MultipleConsentResult>> AddMultipleConsent(MultipleConsentRequest request)
        {
            var iysRequest = new IysRequest<List<Consent>>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/consents/request",
                Body = request.Consents,
                Action = "Add Multiple Consent",
                BatchId = request.BatchId
            };

            return await _clientHelper.Execute<MultipleConsentResult, List<Consent>>(iysRequest);
        }

        public async Task<ResponseBase<List<QueryMultipleConsentResult>>> QueryMultipleConsent(QueryMultipleConsentRequest request)
        {
            var iysRequest = new IysRequest<DummyRequest>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/consents/request/{request.RequestId}",
                Action = "Query Multiple Consent",
                BatchId = request.BatchId
            };

            return await _clientHelper.Execute<List<QueryMultipleConsentResult>, DummyRequest>(iysRequest);
        }

        public async Task<ResponseBase<PullConsentResult>> PullConsent(PullConsentRequest request)
        {
            var iysRequest = new IysRequest<DummyRequest>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/consents/changes?source={request.Source ?? "IYS"}",
                Action = "Pull Consent"
            };

            if (request.Limit > 0)
            {
                iysRequest.Url += $"&limit={request.Limit}";
            }

            if (!string.IsNullOrEmpty(request.After))
            {
                iysRequest.Url += $"&after={request.After}";
            }

            return await _clientHelper.Execute<PullConsentResult, DummyRequest>(iysRequest);
        }

        public ConsentParams GetIysCode(string companyCode)
        {
            return new ConsentParams
            {
                IysCode = _config.GetValue<int>($"{companyCode}:IysCode"),
                BrandCode = _config.GetValue<int>($"{companyCode}:BrandCode")
            };
        }
    }
}
