using IYSIntegration.Common.Worker.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace IYSIntegration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduledController : ControllerBase
    {
        private readonly SingleConsentService _singleConsentService;
        private readonly MultipleConsentService _multipleConsentService;
        private readonly PullConsentService _pullConsentService;
        private readonly SfConsentService _sfConsentService;
        private readonly SendConsentErrorService _sendConsentErrorService;

        public ScheduledController(
            SingleConsentService singleConsentService,
            MultipleConsentService multipleConsentService,
            PullConsentService pullConsentService,
            SfConsentService sfConsentService,
            SendConsentErrorService sendConsentErrorService)
        {
            _singleConsentService = singleConsentService;
            _multipleConsentService = multipleConsentService;
            _pullConsentService = pullConsentService;
            _sfConsentService = sfConsentService;
            _sendConsentErrorService = sendConsentErrorService;
        }

        [HttpPost("single-consent")]
        public async Task<IActionResult> RunSingleConsent()
        {
            await _singleConsentService.ProcessAsync();
            return Ok();
        }

        [HttpPost("multiple-consent")]
        public async Task<IActionResult> RunMultipleConsent()
        {
            await _multipleConsentService.ProcessAsync();
            return Ok();
        }

        [HttpPost("pull-consent")]
        public async Task<IActionResult> RunPullConsent()
        {
            await _pullConsentService.ProcessAsync();
            return Ok();
        }

        [HttpPost("sf-consent")]
        public async Task<IActionResult> RunSfConsent()
        {
            await _sfConsentService.ProcessAsync();
            return Ok();
        }

        [HttpPost("send-consent-error")]
        public async Task<IActionResult> RunSendConsentError()
        {
            await _sendConsentErrorService.ProcessAsync();
            return Ok();
        }
    }
}
