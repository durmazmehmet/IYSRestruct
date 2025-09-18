using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace IYSIntegration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaintenanceController : ControllerBase
    {
        private readonly IDuplicateCleanerService _duplicateCleanerService;
        private readonly IPendingSyncService _pendingSyncService;
        private readonly IOverdueOldConsentsService _overdueOldConsentsService;

        public MaintenanceController(
            IDuplicateCleanerService duplicateCleanerService,
            IPendingSyncService pendingSyncService,
            IOverdueOldConsentsService overdueOldConsentsService)
        {
            _duplicateCleanerService = duplicateCleanerService;
            _pendingSyncService = pendingSyncService;
            _overdueOldConsentsService = overdueOldConsentsService;
        }

        [HttpPost("clean-duplicates")]
        public Task<ResponseBase<ScheduledJobStatistics>> CleanDuplicates()
        {
            return _duplicateCleanerService.RunBatchAsync();
        }

        [HttpPost("sync-pending")]
        public Task<ResponseBase<ScheduledJobStatistics>> SyncPending([FromQuery] int rowCount = 900)
        {
            return _pendingSyncService.RunBatchAsync(rowCount);
        }

        [HttpPost("mark-overdue")]
        public async Task<ResponseBase<int>> MarkOverdue()
        {
            var response = new ResponseBase<int>();
            response.Success();

            var count = await _overdueOldConsentsService.MarkOverdueAsync();
            response.Data = count;
            response.AddMessage("OverdueMarked", count.ToString());

            return response;
        }
    }
}
