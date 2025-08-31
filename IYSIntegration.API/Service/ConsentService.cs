using IYSIntegration.API.Interface;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Common.Response.Consent;

namespace IYSIntegration.API.Service
{
    public class ConsentService : IConsentService
    {
        private readonly IConfiguration _config;
        private readonly IRestClientHelper _clientHelper;
        private readonly string _baseUrl;
        private readonly ICacheService _cacheService;

        public ConsentService(IConfiguration config, IRestClientHelper clientHelper, ICacheService cacheService)
        {
            _config = config;
            _clientHelper = clientHelper;
            _baseUrl = _config.GetValue<string>($"BaseUrl");
            _cacheService = cacheService;
        }

        public async Task<ResponseBase<AddConsentResult>> AddConsent(AddConsentRequest request)
        {
            var iysRequest = new Common.Base.IysRequest<Common.Base.Consent>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/consents",
                Body = request.Consent,
                Action = "Add Consent",
                Method = RestSharp.Method.Post
            };

            return await _clientHelper.Execute<AddConsentResult, Common.Base.Consent>(iysRequest);
        }

        public async Task<ResponseBase<QueryConsentResult>> QueryConsent(QueryConsentRequest request)
        {
            var iysRequest = new Common.Base.IysRequest<Common.Base.RecipientKey>
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
            var iysRequest = new Common.Base.IysRequest<List<Common.Base.Consent>>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/consents/request",
                Body = request.Consents,
                Action = "Add Multiple Consent",
                BatchId = request.BatchId
            };

            return await _clientHelper.Execute<MultipleConsentResult, List<Common.Base.Consent>>(iysRequest);
        }

        public async Task<ResponseBase<List<QueryMultipleConsentResult>>> QueryMultipleConsent(QueryMultipleConsentRequest request)
        {
            var iysRequest = new Common.Base.IysRequest<DummyRequest>
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
            var iysRequest = new Common.Base.IysRequest<DummyRequest>
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

            return await _clientHelper.Execute<PullConsentResult, DummyRequest>(iysRequest); ;
        }
    }
}
