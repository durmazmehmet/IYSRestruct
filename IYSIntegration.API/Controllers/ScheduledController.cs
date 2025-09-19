using IYSIntegration.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace IYSIntegration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduledController : ControllerBase
    {
        private readonly SendConsentToIysService _singleConsentAddService;
        private readonly PullConsentFromIysService _pullConsentService;
        private readonly SendConsentToSalesforceService _sfConsentService;
        private readonly ErrorReportingService _sendConsentErrorService;
        private readonly bool _isMetricsOnline;
        private readonly IConfiguration _configuration;

        public ScheduledController(
                                   SendConsentToIysService singleConsentAddService,
                                   PullConsentFromIysService pullConsentService,
                                   SendConsentToSalesforceService sfConsentService,
                                   ErrorReportingService sendConsentErrorService,
                                   IConfiguration configuration
                                   )
        {
            _singleConsentAddService = singleConsentAddService;
            _pullConsentService = pullConsentService;
            _sfConsentService = sfConsentService;
            _sendConsentErrorService = sendConsentErrorService;
            _configuration = configuration;
            _isMetricsOnline = _configuration.GetValue("IsMetricsOnline", false);
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
            Stopwatch? executionStopwatch = null;
            executionStopwatch.Start();
            var result = await _pullConsentService.RunAsync(batchSize, resetAfter);
            executionStopwatch.Stop();
            result.AddMessage("Execution Time", $"{executionStopwatch.ElapsedMilliseconds} ms");
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
