using IYS.Application.Services;
using IYS.Application.Services.Models.Request;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace IYS.Publisher.API.Controllers
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
                                   ErrorReportingService sendConsentErrorService,
                                   IConfiguration configuration
                                   )
        {
            _singleConsentAddService = singleConsentAddService;
            _pullConsentService = pullConsentService;
            _pullConsentLookupService = pullConsentLookupService;
            _sfConsentService = sfConsentService;
            _sendConsentErrorService = sendConsentErrorService;
        }

        /// <summary>
        /// SF'dan alınan ve IysConsentRequest tablosuna eklenen izinlerin IYS'ye aktarılması
        /// </summary>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        [HttpGet("pushConsentsToIys")]
        public async Task<IActionResult> SingleConsentAdd([FromQuery] int batchSize)
        {

            var result = await _singleConsentAddService.RunAsync(batchSize);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        /// <summary>
        /// IYS'ten izinlerin çekilmesi ve IysPullConsent tablosuna aktarılması
        /// </summary>
        /// <param name="batchSize"></param>
        /// <param name="resetAfter"></param>
        /// <returns></returns>
        [HttpGet("pullConsentFromIys")]
        public async Task<IActionResult> PullConsent([FromQuery] int batchSize, bool resetAfter = false)
        {
            Stopwatch? executionStopwatch = null;
            var result = await _pullConsentService.RunAsync(batchSize, resetAfter);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        /// <summary>
        /// BOD ticari izinlerin IysPullConsentLookup tablosundan verilmesi
        /// </summary>
        /// <param name="dayCount"></param>
        /// <returns></returns>
        [HttpGet("BodCommercialConsents")]
        public async Task<IActionResult> BodCommercialConsents([FromQuery] int dayCount)
        {
            var result = await _pullConsentLookupService.BodCommercialConsents(dayCount);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        /// <summary>
        /// IYS'ten çekilen izinlerin Salesforce'a aktarılması
        /// </summary>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        [HttpGet("pushConsentToSf")]
        public async Task<IActionResult> SfConsent([FromQuery] int batchSize)
        {
            var result = await _sfConsentService.RunAsync(batchSize);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        /// <summary>
        /// Excel Formatında hata raporunun oluşturulması ve BASE64 iletilmesi
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        [HttpGet("GetErrorReportFile")]
        public async Task<IActionResult> GetConsentErrorExcel([FromQuery] DateTime? date)
        {
            var result = await _sendConsentErrorService.GetErrorsExcelBase64Async(date);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }
    }
}
