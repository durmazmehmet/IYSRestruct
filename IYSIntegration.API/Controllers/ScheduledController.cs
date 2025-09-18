using IYSIntegration.Application.Services;
using IYSIntegration.Application.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace IYSIntegration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduledController : ControllerBase
    {
        private readonly ScheduledSingleConsentAddService _singleConsentAddService;
        private readonly ScheduledMultipleConsentAddService _multipleConsentAddService;
        private readonly ScheduledPullConsentService _pullConsentService;
        private readonly ScheduledSfConsentService _sfConsentService;
        private readonly SendConsentErrorService _sendConsentErrorService;
        private readonly IPendingSyncService _pendingSyncService;

        public ScheduledController(
                                   ScheduledSingleConsentAddService singleConsentAddService,
                                   ScheduledMultipleConsentAddService multipleConsentAddService,
                                   ScheduledPullConsentService pullConsentService,
                                   ScheduledSfConsentService sfConsentService,
                                   SendConsentErrorService sendConsentErrorService,
                                   IPendingSyncService pendingSyncService)
        {
            _singleConsentAddService = singleConsentAddService;
            _multipleConsentAddService = multipleConsentAddService;
            _pullConsentService = pullConsentService;
            _sfConsentService = sfConsentService;
            _sendConsentErrorService = sendConsentErrorService;
            _pendingSyncService = pendingSyncService;
        }


        [HttpGet("pushConsentsToIys")]
        public async Task<IActionResult> SingleConsentAdd([FromQuery] int batchSize)
        {
            var result = await _singleConsentAddService.RunAsync(batchSize);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        [HttpGet("pullConsent")]
        public async Task<IActionResult> PullConsent([FromQuery] int batchSize, bool resetAfter = false)
        {
            var result = await _pullConsentService.RunAsync(batchSize, resetAfter);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        [HttpGet("pushConsentToSf")]
        public async Task<IActionResult> SfConsent([FromQuery] int batchSize)
        {
            var result = await _sfConsentService.RunAsync(batchSize);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        [HttpGet("GetErrorReportInExcel")]
        public async Task<IActionResult> GetConsentErrorExcel([FromQuery] DateTime? date)
        {
            var result = await _sendConsentErrorService.GetErrorsExcelBase64Async(date);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }
    }
}
