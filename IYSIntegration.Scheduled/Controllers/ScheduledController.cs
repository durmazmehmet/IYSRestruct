using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using IYSIntegration.Application.Services;

namespace IYSIntegration.Scheduled.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduledController : ControllerBase
    {
        private readonly MultipleConsentQueryService _multipleConsentQueryService;
        private readonly SingleConsentAddService _singleConsentAddService;
        private readonly MultipleConsentAddService _multipleConsentAddService;
        private readonly PullConsentService _pullConsentService;
        private readonly SfConsentService _sfConsentService;
        private readonly SendConsentErrorService _sendConsentErrorService;

        public ScheduledController(MultipleConsentQueryService multipleConsentQueryService,
                                   SingleConsentAddService singleConsentAddService,
                                   MultipleConsentAddService multipleConsentAddService,
                                   PullConsentService pullConsentService,
                                   SfConsentService sfConsentService,
                                   SendConsentErrorService sendConsentErrorService)
        {
            _multipleConsentQueryService = multipleConsentQueryService;
            _singleConsentAddService = singleConsentAddService;
            _multipleConsentAddService = multipleConsentAddService;
            _pullConsentService = pullConsentService;
            _sfConsentService = sfConsentService;
            _sendConsentErrorService = sendConsentErrorService;
        }

        [HttpPost("multiple-consent-query")]
        public async Task<IActionResult> MultipleConsentQuery([FromQuery] int batchCount)
        {
            await _multipleConsentQueryService.RunAsync(batchCount);
            return Ok();
        }

        [HttpPost("multiple-consent-add")]
        public async Task<IActionResult> MultipleConsentAdd([FromQuery] int batchSize, [FromQuery] int batchCount, [FromQuery] int checkAfter)
        {
            await _multipleConsentAddService.RunAsync(batchSize, batchCount, checkAfter);
            return Ok();
        }

        [HttpPost("single-consent-add")]
        public async Task<IActionResult> SingleConsentAdd([FromQuery] int rowCount)
        {
            await _singleConsentAddService.RunAsync(rowCount);
            return Ok();
        }

        [HttpPost("pull-consent")]
        public async Task<IActionResult> PullConsent([FromQuery] int batchSize)
        {
            await _pullConsentService.RunAsync(batchSize);
            return Ok();
        }

        [HttpPost("sf-consent")]
        public async Task<IActionResult> SfConsent([FromQuery] int rowCount)
        {
            await _sfConsentService.RunAsync(rowCount);
            return Ok();
        }

        [HttpPost("send-consent-error")]
        public async Task<IActionResult> SendConsentError()
        {
            await _sendConsentErrorService.RunAsync();
            return Ok();
        }
    }
}
