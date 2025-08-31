using IYSIntegration.Application.Interface;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Common.Response.Consent;
using Microsoft.AspNetCore.Mvc;
using WorkerPullConsentService = IYSIntegration.Common.Worker.Services.PullConsentService;
using WorkerSfConsentService = IYSIntegration.Common.Worker.Services.SfConsentService;

namespace IYSIntegration.API.Controllers
{
    [ApiController]
    [Route("api/consent")]
    public class ConsentApiPullController : ControllerBase
    {
        private readonly IConsentService _consentManager;
        private readonly ISfConsentService _sfConsentManager;
        private readonly WorkerPullConsentService _pullConsentService;
        private readonly WorkerSfConsentService _sfConsentWorkerService;

        public ConsentApiPullController(
            IConsentService consentManager,
            ISfConsentService sfConsentManager,
            WorkerPullConsentService pullConsentService,
            WorkerSfConsentService sfConsentService)
        {
            _consentManager = consentManager;
            _sfConsentManager = sfConsentManager;
            _pullConsentService = pullConsentService;
            _sfConsentWorkerService = sfConsentService;
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

        [HttpPost("pull-consent")]
        public async Task<IActionResult> RunPullConsent()
        {
            var result = await _pullConsentService.ProcessAsync();
            return Ok(result);
        }

        [HttpPost("sf-consent")]
        public async Task<IActionResult> RunSfConsent()
        {
            var result = await _sfConsentWorkerService.ProcessAsync();
            return Ok(result);
        }
    }
}

