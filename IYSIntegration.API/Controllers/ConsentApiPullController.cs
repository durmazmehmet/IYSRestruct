using IYSIntegration.API.Interface;
using IYSIntegration.Common.Request.Consent;
using Microsoft.AspNetCore.Mvc;

namespace IYSIntegration.API.Controllers
{
    [ApiController]
    [Route("api/consent/{iysCode}/brands/{brandCode}")]
    public class ConsentApiPullController : ControllerBase
    {
        private readonly IConsentService _consentManager;
        public ConsentApiPullController(IConsentService consentManager)
        {
            _consentManager = consentManager;
        }

        [Route("consents/changes")]
        [HttpGet]
        public async Task<IActionResult> PullConsent(int iysCode, int brandCode, string after, int limit, string source)
        {
            var request = new PullConsentRequest { IysCode = iysCode, BrandCode = brandCode, After = after, Limit = limit, Source = source };
            var result = await _consentManager.PullConsent(request);
            return Ok(result);
        }
    }
}

