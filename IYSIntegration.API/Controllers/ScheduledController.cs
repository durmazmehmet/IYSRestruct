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

        public ScheduledController(ScheduledMultipleConsentQueryService multipleConsentQueryService,
                                   ScheduledSingleConsentAddService singleConsentAddService,
                                   ScheduledMultipleConsentAddService multipleConsentAddService,
                                   ScheduledPullConsentService pullConsentService,
                                   ScheduledSfConsentService sfConsentService,
                                   ScheduledSendConsentErrorService sendConsentErrorService)
        {
            _multipleConsentQueryService = multipleConsentQueryService;
            _singleConsentAddService = singleConsentAddService;
            _multipleConsentAddService = multipleConsentAddService;
            _pullConsentService = pullConsentService;
            _sfConsentService = sfConsentService;
            _sendConsentErrorService = sendConsentErrorService;
        }

        /// <summary>
        /// IYS'den toplu rıza sorgulama sonuçları çekilir ve DB'de güncellenir.
        /// </summary>
        /// <param name="batchCount"></param>
        /// <returns></returns>
        [HttpPost("multiple-consent-query")]
        public async Task<IActionResult> MultipleConsentQuery([FromQuery] int batchCount)
        {
            var result = await _multipleConsentQueryService.RunAsync(batchCount);
            if (!result.IsSuccessful())
                return StatusCode(500, result);
            return Ok(result);
        }

        /// <summary>
        /// SF'dan gelip sıralanan rızalar toplu IYS'ye eklenir.
        /// </summary>
        /// <param name="batchSize"></param>
        /// <param name="batchCount"></param>
        /// <param name="checkAfter"></param>
        /// <returns></returns>
        [HttpPost("pushBulkConsentToIys")]
        public async Task<IActionResult> MultipleConsentAdd([FromQuery] int batchSize, [FromQuery] int batchCount, [FromQuery] int checkAfter)
        {
            var result = await _multipleConsentAddService.RunAsync(batchSize, batchCount, checkAfter);
            if (!result.IsSuccessful())
                return StatusCode(500, result);
            return Ok(result);
        }

        /// <summary>
        /// SF'dan gelip sıralanan rızalar tek tek IYS'ye eklenir.
        /// </summary>
        /// <param name="rowCount"></param>
        /// <returns></returns>
        [HttpPost("pushConsentsToIys")]
        public async Task<IActionResult> SingleConsentAdd([FromQuery] int rowCount)
        {
            var result = await _singleConsentAddService.RunAsync(rowCount);
            if (!result.IsSuccessful())
                return StatusCode(500, result);
            return Ok(result);
        }
        /// <summary>
        /// IYS'den gelen rıza kayıtları çekilir ve DB'de saklanır.
        /// </summary>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        [HttpGet("pullconsent")]
        public async Task<IActionResult> PullConsent([FromQuery] int batchSize, bool resetAfter = false)
        {
            var result = await _pullConsentService.RunAsync(batchSize, resetAfter);
            if (!result.IsSuccessful())
                return StatusCode(500, result);
            return Ok(result);
        }
        /// <summary>
        /// IYS'den toplanan rıza kayıtları ve SF'a aktarılır.
        /// </summary>
        /// <param name="rowCount"></param>
        /// <returns></returns>
        [HttpPost("pushConsentToSF")]
        public async Task<IActionResult> SfConsent([FromQuery] int rowCount)
        {
            var result = await _sfConsentService.RunAsync(rowCount);
            if (!result.IsSuccessful())
                return StatusCode(500, result);
            return Ok(result);
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
            return Ok(result);
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
            return Ok(result);
        }
    }
}
