using IYSIntegration.Application.Base;
using IYSIntegration.Application.Request.Consent;
using IYSIntegration.Application.Response.Consent;

namespace IYSIntegration.Application.Services.Interface
{
    public interface IConsentService
    {
        Task<ResponseBase<AddConsentResult>> AddConsent(AddConsentRequest consent);
        Task<ResponseBase<QueryConsentResult>> QueryConsent(QueryConsentRequest request);
        Task<ResponseBase<MultipleConsentResult>> AddMultipleConsent(MultipleConsentRequest request);
        Task<ResponseBase<List<QueryMultipleConsentResult>>> QueryMultipleConsent(QueryMultipleConsentRequest request);
        Task<ResponseBase<PullConsentResult>> PullConsent(PullConsentRequest request);
        ConsentParams GetIysCode(string companyCode);
    }
}

