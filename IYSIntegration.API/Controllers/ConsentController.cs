using IYSIntegration.Application.Interface;
using IYSIntegration.Common.Request.Consent;
using Microsoft.AspNetCore.Mvc;

namespace IYSIntegration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]/{iysCode}/brands/{brandCode}")]
    public class ConsentController : ControllerBase
    {
        private readonly IConsentService _consentManager;
        public ConsentController(IConsentService consentManager)
        {
            _consentManager = consentManager;
        }

        [Route("consents")]
        [HttpPost]
        public async Task<IActionResult> AddConsent(int iysCode, int brandCode, [FromBody] Common.Base.Consent consent)
        {
            var request = new AddConsentRequest { IysCode = iysCode, BrandCode = brandCode, Consent = consent };
            var result = await _consentManager.AddConsent(request);
            return Ok(result);
        }

        [Route("consents/status")]
        [HttpPost]
        public async Task<IActionResult> SearchConsent(int iysCode, int brandCode, [FromBody] Common.Base.RecipientKey recipientKey)
        {
            var request = new QueryConsentRequest { IysCode = iysCode, BrandCode = brandCode, RecipientKey = recipientKey };
            var result = await _consentManager.QueryConsent(request);
            return Ok(result);
        }


        [Route("consents/request")]
        [HttpPost]
        public async Task<IActionResult> AddMultipleConsent(int iysCode, int brandCode, [FromBody] List<Common.Base.Consent> consents)
        {
            var request = new MultipleConsentRequest { IysCode = iysCode, BrandCode = brandCode, Consents = consents };
            var result = await _consentManager.AddMultipleConsent(request);
            return Ok(result);
        }


        [Route("consents/request/{requestId}")]
        [HttpGet]
        public async Task<IActionResult> SearchMultipleConsent(int iysCode, int brandCode, string requestId)
        {
            var request = new QueryMultipleConsentRequest { IysCode = iysCode, BrandCode = brandCode, RequestId = requestId };
            var result = await _consentManager.QueryMultipleConsent(request);
            return Ok(result);
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
