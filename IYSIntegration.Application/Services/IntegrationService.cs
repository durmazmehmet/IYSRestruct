using IYSIntegration.Application.Interface;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Common.Response.Consent;
using Microsoft.Extensions.Logging;

namespace IYSIntegration.Application.Services
{
    public class IntegrationService : IIntegrationService
    {
        private readonly IConsentService _consentService;
        private readonly IDbService _dbService;
        private readonly ISfConsentService _sfConsentService;
        private readonly ILogger<IntegrationService> _logger;

        public IntegrationService(
            IConsentService consentService,
            IDbService dbService,
            ISfConsentService sfConsentService,
            ILogger<IntegrationService> logger)
        {
            _consentService = consentService;
            _dbService = dbService;
            _sfConsentService = sfConsentService;
            _logger = logger;
        }

        public async Task<ResponseBase<AddConsentResult>> AddConsent(AddConsentRequest request)
        {
            if (request.IysCode == 0)
            {
                var consentParams = _consentService.GetIysCode(request.CompanyCode);
                request.IysCode = consentParams.IysCode;
                request.BrandCode = consentParams.BrandCode;
            }

            if (!request.WithoutLogging)
            {
                var id = await _dbService.InsertConsentRequest(request);
                var response = await _consentService.AddConsent(request);
                response.Id = id;
                await _dbService.UpdateConsentResponseFromCommon(response);
                response.OriginalError = null;
                return response;
            }

            return await _consentService.AddConsent(request);
        }

        public async Task<ResponseBase<MultipleConsentResult>> SendMultipleConsent(MultipleConsentRequest request)
        {
            if (request.IysCode == 0)
            {
                var consentParams = _consentService.GetIysCode(request.CompanyCode);
                request.IysCode = consentParams.IysCode;
                request.BrandCode = consentParams.BrandCode;
            }

            return await _consentService.AddMultipleConsent(request);
        }

        public async Task<ResponseBase<List<QueryMultipleConsentResult>>> QueryMultipleConsent(QueryMultipleConsentRequest request)
        {
            if (request.IysCode == 0)
            {
                var consentParams = _consentService.GetIysCode(request.CompanyCode);
                request.IysCode = consentParams.IysCode;
                request.BrandCode = consentParams.BrandCode;
            }

            return await _consentService.QueryMultipleConsent(request);
        }

        public async Task<ResponseBase<PullConsentResult>> PullConsent(PullConsentRequest request)
        {
            if (request.IysCode == 0)
            {
                var consentParams = _consentService.GetIysCode(request.CompanyCode);
                request.IysCode = consentParams.IysCode;
                request.BrandCode = consentParams.BrandCode;
            }

            return await _consentService.PullConsent(request);
        }

        public async Task<SfConsentAddResponse> SfAddConsent(SfConsentAddRequest request)
        {
            return await _sfConsentService.AddConsent(request);
        }
    }
}

