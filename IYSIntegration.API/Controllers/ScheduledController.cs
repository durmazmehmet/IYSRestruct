using IYSIntegration.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IYSIntegration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduledController : ControllerBase
    {
        private readonly SendConsentToIysService _singleConsentAddService;
        private readonly PullConsentFromIysService _pullConsentService;
        private readonly PullConsentLookupService _pullConsentLookupService;
        private readonly SendConsentToSalesforceService _sfConsentService;
        private readonly ErrorReportingService _sendConsentErrorService;

        public ScheduledController(
                                   SendConsentToIysService singleConsentAddService,
                                   PullConsentFromIysService pullConsentService,
                                   PullConsentLookupService pullConsentLookupService,
                                   SendConsentToSalesforceService sfConsentService,
                                   ErrorReportingService sendConsentErrorService
                                   )
        {
            _singleConsentAddService = singleConsentAddService;
            _pullConsentService = pullConsentService;
            _pullConsentLookupService = pullConsentLookupService;
            _sfConsentService = sfConsentService;
            _sendConsentErrorService = sendConsentErrorService;
        }


        [HttpGet("pushConsentsToIys")]
        public async Task<IActionResult> SingleConsentAdd([FromQuery] int batchSize)
        {
            var result = await _singleConsentAddService.RunAsync(batchSize);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        [HttpGet("pullConsentFromIys")]
        public async Task<IActionResult> PullConsent([FromQuery] int batchSize, bool resetAfter = false)
        {
            var result = await _pullConsentService.RunAsync(batchSize, resetAfter);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        [HttpGet("getTacirPullConsents")]
        public async Task<IActionResult> GetTacirPullConsents([FromQuery] int dayCount)
        {
            var result = await _pullConsentLookupService.GetRecentConsentsAsync(dayCount);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        [HttpGet("pushConsentToSf")]
        public async Task<IActionResult> SfConsent([FromQuery] int batchSize)
        {
            var result = await _sfConsentService.RunAsync(batchSize);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        [HttpGet("GetErrorReportFile")]
        public async Task<IActionResult> GetConsentErrorExcel([FromQuery] DateTime? date)
        {
            var result = await _sendConsentErrorService.GetErrorsExcelBase64Async(date);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }
    }
}
