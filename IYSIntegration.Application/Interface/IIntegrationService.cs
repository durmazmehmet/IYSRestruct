using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Common.Response.Consent;

namespace IYSIntegration.Application.Interface
{
    public interface IIntegrationService
    {
        Task<ResponseBase<AddConsentResult>> AddConsent(AddConsentRequest request);
        Task<ResponseBase<MultipleConsentResult>> SendMultipleConsent(MultipleConsentRequest request);
        Task<ResponseBase<List<QueryMultipleConsentResult>>> QueryMultipleConsent(QueryMultipleConsentRequest request);
        Task<ResponseBase<PullConsentResult>> PullConsent(PullConsentRequest request);
        Task<SfConsentAddResponse> SfAddConsent(SfConsentAddRequest request);
    }
}
