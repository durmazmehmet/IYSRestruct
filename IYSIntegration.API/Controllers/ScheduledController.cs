using IYSIntegration.Common.Worker.Services;
using IYSIntegration.Common.Worker.Models;
using IYSIntegration.Common.Base;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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
        public async Task<ActionResult<WorkerResult>> RunSingleConsent()
        {
            var result = await _singleConsentService.ProcessAsync();
            return Ok(result);
        }

        [HttpPost("multiple-consent")]
        public async Task<ActionResult<WorkerResult>> RunMultipleConsent()
        {
            var result = await _multipleConsentService.ProcessAsync();
            return Ok(result);
        }

        [HttpPost("pull-consent")]
        public async Task<ActionResult<WorkerResult>> RunPullConsent()
        {
            var result = await _pullConsentService.ProcessAsync();
            return Ok(result);
        }

        [HttpPost("sf-consent")]
        public async Task<ActionResult<WorkerResult>> RunSfConsent()
        {
            var result = await _sfConsentService.ProcessAsync();
            return Ok(result);
        }

        [HttpGet("send-consent-error/report-excel")]
        public async Task<ActionResult<string?>> GetConsentErrorReportExcel([FromQuery] DateTime date)
        {
            var result = await _sendConsentErrorService.ReportExcelAsync(date);
            return Ok(result);
        }

        [HttpGet("send-consent-error/report")]
        public async Task<ActionResult<List<Consent>>> GetConsentErrorReport([FromQuery] DateTime date)
        {
            var result = await _sendConsentErrorService.ReportAsync(date);
            return Ok(result);
        }
    }
}
