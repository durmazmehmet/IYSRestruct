using IYSIntegration.Application.Request.Consent;
using IYSIntegration.Application.Response.Consent;

namespace IYSIntegration.Application.Services.Interface
{
    public interface ISfConsentService
    {
        Task<SfConsentAddResponse> AddConsent(SfConsentAddRequest request);
    }
}
