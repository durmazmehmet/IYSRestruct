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

        public ConsentScheduleController(
            SingleConsentService singleConsentService,
            MultipleConsentService multipleConsentService)
        {
            _singleConsentService = singleConsentService;
            _multipleConsentService = multipleConsentService;
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

    }
}
