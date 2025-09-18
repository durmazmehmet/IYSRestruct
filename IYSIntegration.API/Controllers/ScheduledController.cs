using IYSIntegration.Application.Services;
using IYSIntegration.Application.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace IYSIntegration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduledController : ControllerBase
    {
        private readonly ScheduledMultipleConsentQueryService _multipleConsentQueryService;
        private readonly ScheduledSingleConsentAddService _singleConsentAddService;
        private readonly ScheduledMultipleConsentAddService _multipleConsentAddService;
        private readonly ScheduledPullConsentService _pullConsentService;
        private readonly ScheduledSfConsentService _sfConsentService;
        private readonly ScheduledSendConsentErrorService _sendConsentErrorService;
        private readonly IPendingSyncService _pendingSyncService;

        public ScheduledController(ScheduledMultipleConsentQueryService multipleConsentQueryService,
                                   ScheduledSingleConsentAddService singleConsentAddService,
                                   ScheduledMultipleConsentAddService multipleConsentAddService,
                                   ScheduledPullConsentService pullConsentService,
                                   ScheduledSfConsentService sfConsentService,
                                   ScheduledSendConsentErrorService sendConsentErrorService,
                                   IPendingSyncService pendingSyncService)
        {
            _multipleConsentQueryService = multipleConsentQueryService;
            _singleConsentAddService = singleConsentAddService;
            _multipleConsentAddService = multipleConsentAddService;
            _pullConsentService = pullConsentService;
            _sfConsentService = sfConsentService;
            _sendConsentErrorService = sendConsentErrorService;
            _pendingSyncService = pendingSyncService;
        }

        [HttpGet("bulkConsentQuery")]
        public async Task<IActionResult> MultipleConsentQuery([FromQuery] int batchSize)
        {
            var result = await _multipleConsentQueryService.RunAsync(batchSize);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        [HttpGet("pushBulkConsentToIys")]
        public async Task<IActionResult> MultipleConsentAdd([FromQuery] int batchSize, int diffInSeconds)
        {
            var result = await _multipleConsentAddService.RunAsync(batchSize, diffInSeconds);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
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

        [HttpGet("syncPendingConsents")]
        public async Task<IActionResult> SyncPendingConsents([FromQuery] int batchSize = 900)
        {
            var result = await _pendingSyncService.RunBatchAsync(batchSize);
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

        [HttpGet("GetErrorReport")]
        public async Task<IActionResult> GetConsentErrorJson([FromQuery] DateTime? date)
        {
            var result = await _sendConsentErrorService.GetErrorsJsonAsync(date);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }
    }
}
