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
        private readonly SendMultipleConsentToIysService _multipleConsentService;
        private readonly PullConsentFromIysService _pullConsentService;
        private readonly PullConsentLookupService _pullConsentLookupService;
        private readonly SendConsentToSalesforceService _sfConsentService;
        private readonly ErrorReportingService _sendConsentErrorService;

        public ScheduledController(
                                   SendConsentToIysService singleConsentAddService,
                                   SendMultipleConsentToIysService multipleConsentService,
                                   PullConsentFromIysService pullConsentService,
                                   PullConsentLookupService pullConsentLookupService,
                                   SendConsentToSalesforceService sfConsentService,
                                   ErrorReportingService sendConsentErrorService,
                                   IConfiguration configuration
                                   )
        {
            _singleConsentAddService = singleConsentAddService;
            _multipleConsentService = multipleConsentService;
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
        /// IysConsentRequest tablosundaki kayıtları çoklu ekleme servisi ile IYS'ye gönderir.
        /// </summary>
        /// <param name="batchSize">Gönderilecek batch sayısı.</param>
        /// <returns></returns>
        [HttpGet("pushMultipleConsentsToIys")]
        public async Task<IActionResult> PushMultipleConsents([FromQuery] int batchSize)
        {
            var result = await _multipleConsentService.SendPendingBatchesAsync(batchSize);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        /// <summary>
        /// Çoklu ekleme isteklerinin durumunu sorgular ve sonuçlarını günceller.
        /// </summary>
        /// <param name="batchSize">Sorgulanacak batch sayısı.</param>
        /// <returns></returns>
        [HttpGet("queryMultipleConsentBatches")]
        public async Task<IActionResult> QueryMultipleConsentBatches([FromQuery] int batchSize)
        {
            var result = await _multipleConsentService.QueryPendingBatchesAsync(batchSize);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        /// <summary>
        /// Dışarıdan gelen çoklu rıza isteğini doğrudan IYS'ye iletir.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("sendMultipleConsents")]
        public async Task<IActionResult> SendMultipleConsents([FromBody] MultipleConsentRequest request)
        {
            var result = await _multipleConsentService.SendFromRequestAsync(request);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        /// <summary>
        /// Çoklu ekleme isteğinin durumunu sorgular.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("queryMultipleConsentStatus")]
        public async Task<IActionResult> QueryMultipleConsentStatus([FromBody] MultipleConsentRequest request)
        {
            var result = await _multipleConsentService.QueryStatusFromRequestAsync(request);
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
