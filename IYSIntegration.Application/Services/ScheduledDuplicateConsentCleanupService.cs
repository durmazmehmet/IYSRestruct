using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using Microsoft.Extensions.Logging;

namespace IYSIntegration.Application.Services
{
    public class ScheduledDuplicateConsentCleanupService
    {
        private readonly ILogger<ScheduledDuplicateConsentCleanupService> _logger;
        private readonly IDbService _dbService;

        public ScheduledDuplicateConsentCleanupService(
            ILogger<ScheduledDuplicateConsentCleanupService> logger,
            IDbService dbService)
        {
            _logger = logger;
            _dbService = dbService;
        }

        public async Task<ResponseBase<ScheduledJobStatistics>> RunAsync()
        {
            var response = new ResponseBase<ScheduledJobStatistics>();
            response.Success();

            try
            {
                var affected = await _dbService.MarkDuplicateConsentsOverdue();

                response.Data = new ScheduledJobStatistics
                {
                    SuccessCount = affected,
                    FailedCount = 0
                };

                response.AddMessage("DuplicatesMarked", affected.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Duplicate consent cleanup failed.");
                response.Error("DUPLICATE_MARK_FAILED", "Tekrarlanan kayıtlar gecikmiş olarak işaretlenemedi.");
            }

            return response;
        }
    }
}
