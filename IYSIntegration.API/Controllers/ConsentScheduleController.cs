using IYSIntegration.Common.Worker.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace IYSIntegration.API.Controllers
{
    [ApiController]
    [Route("api/consent-schedule")]
    public class ConsentScheduleController : ControllerBase
    {
        private readonly SingleConsentService _singleConsentService;
        private readonly MultipleConsentService _multipleConsentService;
        private readonly PullConsentService _pullConsentService;
        private readonly SfConsentService _sfConsentService;

        public ConsentScheduleController(
            SingleConsentService singleConsentService,
            MultipleConsentService multipleConsentService,
            PullConsentService pullConsentService,
            SfConsentService sfConsentService)
        {
            _singleConsentService = singleConsentService;
            _multipleConsentService = multipleConsentService;
            _pullConsentService = pullConsentService;
            _sfConsentService = sfConsentService;
        }

        [HttpPost("single-consent")]
        public async Task<IActionResult> RunSingleConsent()
        {
            var result = await _singleConsentService.ProcessAsync();
            return Ok(result);
        }

        [HttpPost("multiple-consent")]
        public async Task<IActionResult> RunMultipleConsent()
        {
            var result = await _multipleConsentService.ProcessAsync();
            return Ok(result);
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
            var result = await _sfConsentService.ProcessAsync();
            return Ok(result);
        }
    }
}
