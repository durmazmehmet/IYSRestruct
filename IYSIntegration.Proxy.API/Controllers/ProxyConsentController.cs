using System.Collections.Generic;
using IYSIntegration.Application.Interface;
using IYSIntegration.Common.Request.Consent;
using Microsoft.AspNetCore.Mvc;

namespace IYSIntegration.Proxy.API.Controllers
{
    [ApiController]
    [Route("api/[controller]/{iysCode}/brands/{brandCode}")]
    public class ProxyConsentController : ControllerBase
    {
        private readonly IConsentService _consentService;

        public ProxyConsentController(IConsentService consentService)
        {
            _consentService = consentService;
        }

        [Route("consents")]
        [HttpPost]
        public async Task<IActionResult> AddConsent(int iysCode, int brandCode, [FromBody] Common.Base.Consent consent)
        {
            var request = new AddConsentRequest { IysCode = iysCode, BrandCode = brandCode, Consent = consent };
            var result = await _consentService.AddConsent(request);
            return Ok(result);
        }

        [Route("consents/status")]
        [HttpPost]
        public async Task<IActionResult> SearchConsent(int iysCode, int brandCode, [FromBody] Common.Base.RecipientKey recipientKey)
        {
            var request = new QueryConsentRequest { IysCode = iysCode, BrandCode = brandCode, RecipientKey = recipientKey };
            var result = await _consentService.QueryConsent(request);
            return Ok(result);
        }

        [Route("consents/request")]
        [HttpPost]
        public async Task<IActionResult> AddMultipleConsent(int iysCode, int brandCode, [FromBody] List<Common.Base.Consent> consents)
        {
            var request = new MultipleConsentRequest { IysCode = iysCode, BrandCode = brandCode, Consents = consents };
            var result = await _consentService.AddMultipleConsent(request);
            return Ok(result);
        }

        [Route("consents/request/{requestId}")]
        [HttpGet]
        public async Task<IActionResult> SearchMultipleConsent(int iysCode, int brandCode, string requestId)
        {
            var request = new QueryMultipleConsentRequest { IysCode = iysCode, BrandCode = brandCode, RequestId = requestId };
            var result = await _consentService.QueryMultipleConsent(request);
            return Ok(result);
        }

        [Route("consents/changes")]
        [HttpGet]
        public async Task<IActionResult> PullConsent(int iysCode, int brandCode, string after, int limit, string source)
        {
            var request = new PullConsentRequest { IysCode = iysCode, BrandCode = brandCode, After = after, Limit = limit, Source = source };
            var result = await _consentService.PullConsent(request);
            return Ok(result);
        }
    }
}
