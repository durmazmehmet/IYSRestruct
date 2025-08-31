using IYSIntegration.Application.Interface;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Common.Response.Consent;
using Microsoft.AspNetCore.Mvc;

namespace IYSIntegration.API.Controllers
{
    [ApiController]
    [Route("api/consent")]
    public class ConsentApiPullController : ControllerBase
    {
        private readonly IConsentService _consentManager;
        private readonly ISfConsentService _sfConsentManager;

        public ConsentApiPullController(IConsentService consentManager, ISfConsentService sfConsentManager)
        {
            _consentManager = consentManager;
            _sfConsentManager = sfConsentManager;
        }

        [HttpGet("{companyCode}/consents/changes")]
        public async Task<IActionResult> PullConsent(string companyCode)
        {
            var result = await _consentManager.PullConsent(companyCode);
            return Ok(result);
        }

        [HttpPost("sfaddconsent")]
        public async Task<SfConsentAddResponse> SalesforceAddConsent(SfConsentAddRequest request)
        {
            return await _sfConsentManager.AddConsent(request);
        }
    }
}

