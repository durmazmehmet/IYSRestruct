using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Common.Response.Consent;

namespace IYSIntegration.Application.Interface
{
    public interface ISfConsentService
    {
        Task<SfConsentAddResponse> AddConsent(SfConsentAddRequest request);
    }
}
