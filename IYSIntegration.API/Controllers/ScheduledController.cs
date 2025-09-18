using IYSIntegration.Application.Services;
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
        private readonly ScheduledPendingConsentSyncService _pendingConsentSyncService;
        private readonly ScheduledConsentOverdueService _consentOverdueService;

        public ScheduledController(ScheduledMultipleConsentQueryService multipleConsentQueryService,
                                   ScheduledSingleConsentAddService singleConsentAddService,
                                   ScheduledMultipleConsentAddService multipleConsentAddService,
                                   ScheduledPullConsentService pullConsentService,
                                   ScheduledSfConsentService sfConsentService,
                                   ScheduledSendConsentErrorService sendConsentErrorService,
                                   ScheduledPendingConsentSyncService pendingConsentSyncService,
                                   ScheduledConsentOverdueService consentOverdueService)
        {
            _multipleConsentQueryService = multipleConsentQueryService;
            _singleConsentAddService = singleConsentAddService;
            _multipleConsentAddService = multipleConsentAddService;
            _pullConsentService = pullConsentService;
            _sfConsentService = sfConsentService;
            _sendConsentErrorService = sendConsentErrorService;
            _pendingConsentSyncService = pendingConsentSyncService;
            _consentOverdueService = consentOverdueService;
        }

        /// <summary>
        /// IYS'den toplu rıza sorgulama sonuçları çekilir ve DB'de güncellenir.
        /// </summary>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        [HttpGet("bulkConsentQuery")]
        public async Task<IActionResult> MultipleConsentQuery([FromQuery] int batchSize)
        {
            var result = await _multipleConsentQueryService.RunAsync(batchSize);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        /// <summary>
        /// SF'dan gelip sıralanan rızalar toplu IYS'ye eklenir.
        /// </summary>
        /// <param name="batchSize"></param>
        /// <param name="diffInSeconds"></param>
        /// <returns></returns>
        [HttpGet("pushBulkConsentToIys")]
        public async Task<IActionResult> MultipleConsentAdd([FromQuery] int batchSize, int diffInSeconds)
        {
            var result = await _multipleConsentAddService.RunAsync(batchSize, diffInSeconds);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        /// <summary>
        /// SF'dan gelip sıralanan rızalar tek tek IYS'ye eklenir.
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
        /// IYS'den gelen rıza kayıtları çekilir ve DB'de saklanır.
        /// </summary>
        /// <param name="batchSize"></param>
        /// <param name="resetAfter"></param>
        /// <returns></returns>
        [HttpGet("pullConsent")]
        public async Task<IActionResult> PullConsent([FromQuery] int batchSize, bool resetAfter = false)
        {
            var result = await _pullConsentService.RunAsync(batchSize, resetAfter);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }
        /// <summary>
        /// IYS'den toplanan rıza kayıtları ve SF'a aktarılır.
        /// </summary>
        /// <param name="batchCount"></param>
        /// <returns></returns>
        [HttpGet("pushConsentToSf")]
        public async Task<IActionResult> SfConsent([FromQuery] int batchSize)
        {
            var result = await _sfConsentService.RunAsync(batchSize);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        /// <summary>
        /// Bekleyen rızalar IYS'den sorgulanır ve sisteme eklenir.
        /// </summary>
        /// <param name="rowCount"></param>
        /// <returns></returns>
        [HttpGet("syncPendingConsents")]
        public async Task<IActionResult> SyncPendingConsents([FromQuery] int rowCount)
        {
            var result = await _pendingConsentSyncService.RunAsync(rowCount);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        /// <summary>
        /// Bekleyen rızalar gecikmiş olarak işaretlenir.
        /// </summary>
        /// <param name="maxAgeInDays"></param>
        /// <returns></returns>
        [HttpGet("markPendingConsentsOverdue")]
        public async Task<IActionResult> MarkPendingConsentsOverdue([FromQuery] int maxAgeInDays = 3)
        {
            var result = await _consentOverdueService.RunAsync(maxAgeInDays);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        /// <summary>
        /// Hata raporu excel olarak çekilir
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        [HttpGet("GetErrorReportInExcel")]
        public async Task<IActionResult> GetConsentErrorExcel([FromQuery] DateTime? date)
        {
            var result = await _sendConsentErrorService.GetErrorsExcelBase64Async(date);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        /// <summary>
        /// Hata raporu json olarak çekilir
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        [HttpGet("GetErrorReport")]
        public async Task<IActionResult> GetConsentErrorJson([FromQuery] DateTime? date)
        {
            var result = await _sendConsentErrorService.GetErrorsJsonAsync(date);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }
    }
}
