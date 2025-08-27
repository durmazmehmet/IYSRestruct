using IYSIntegration.Common.Worker.Services;
using Microsoft.AspNetCore.Mvc;
using System;
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

        [HttpPost("send-consent-error/excel")]
        public async Task<IActionResult> RunSendConsentErrorExcel([FromQuery] DateTime? date)
        {
            var result = await _sendConsentErrorService.GetReportExcelAsync(date ?? DateTime.Today);
            return Ok(result);
        }

        [HttpPost("send-consent-error/json")]
        public async Task<IActionResult> RunSendConsentErrorJson([FromQuery] DateTime? date)
        {
            var result = await _sendConsentErrorService.GetReportJsonAsync(date ?? DateTime.Today);
            return Ok(result);
        }
    }
}
