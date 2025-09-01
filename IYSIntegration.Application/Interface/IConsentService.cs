using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Common.Response.Consent;

namespace IYSIntegration.Application.Interface
{
    public interface IConsentService
    {
        Task<ResponseBase<AddConsentResult>> AddConsent(AddConsentRequest consent);
        Task<ResponseBase<QueryConsentResult>> QueryConsent(QueryConsentRequest request);
        Task<ResponseBase<MultipleConsentResult>> AddMultipleConsent(MultipleConsentRequest request);
        Task<ResponseBase<List<QueryMultipleConsentResult>>> QueryMultipleConsent(QueryMultipleConsentRequest request);
        Task<ResponseBase<PullConsentResult>> PullConsent(PullConsentRequest request);
    }
}

