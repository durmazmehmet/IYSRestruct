using IYSIntegration.Application.Interface;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.Error;
using IYSIntegration.Common.Request;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Common.Response.Consent;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using IYSIntegration.Common.Services;

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
            if (!string.IsNullOrWhiteSpace(request.Consent?.ConsentDate) &&
                DateTime.TryParse(request.Consent.ConsentDate, out var consentDate) &&
                IsOlderThanBusinessDays(consentDate, 3))
            {
                var response = new ResponseBase<AddConsentResult>
                {
                    HttpStatusCode = 200,
                    OriginalError = new GenericError
                    {
                        Message = "Consent older than three business days",
                        Status = 400,
                        Errors = new[]
                        {
                            new ErrorDetails
                            {
                                Code = "H174",
                                Message = "Consent older than three business days"
                            }
                        }
                    }
                };

                response.Error("ConsentDate", "Consent older than three business days");
                return response;
            }

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

        private static bool IsOlderThanBusinessDays(DateTime consentDate, int maxBusinessDays)
        {
            var date = consentDate.Date;
            var today = DateTime.Now.Date;
            int businessDays = 0;

            while (date < today)
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    businessDays++;
                }

                if (businessDays >= maxBusinessDays)
                {
                    return true;
                }

                date = date.AddDays(1);
            }

            return false;
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
    }
}
