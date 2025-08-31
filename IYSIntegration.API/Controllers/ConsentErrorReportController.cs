using IYSIntegration.Common.Worker.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace IYSIntegration.API.Controllers
{
    [ApiController]
    [Route("api/consent-error-report")]
    public class ConsentErrorReportController : ControllerBase
    {
        private readonly SendConsentErrorService _sendConsentErrorService;

        public ConsentErrorReportController(SendConsentErrorService sendConsentErrorService)
        {
            _sendConsentErrorService = sendConsentErrorService;
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
