using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using Microsoft.Extensions.Logging;

namespace IYSIntegration.Application.Services
{
    public class ScheduledConsentOverdueService
    {
        private readonly ILogger<ScheduledConsentOverdueService> _logger;
        private readonly IDbService _dbService;

        public ScheduledConsentOverdueService(
            ILogger<ScheduledConsentOverdueService> logger,
            IDbService dbService)
        {
            _logger = logger;
            _dbService = dbService;
        }

        public async Task<ResponseBase<ScheduledJobStatistics>> RunAsync(int maxAgeInDays = 3)
        {
            var response = new ResponseBase<ScheduledJobStatistics>();
            response.Success();

            try
            {
                var affected = await _dbService.MarkConsentsOverdue(maxAgeInDays);

                response.Data = new ScheduledJobStatistics
                {
                    SuccessCount = affected,
                    FailedCount = 0
                };

                response.AddMessage("OverdueMarked", affected.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pending consents could not be marked as overdue.");
                response.Error("OVERDUE_MARK_FAILED", "Bekleyen kayıtlar gecikmiş olarak işaretlenemedi.");
            }

            return response;
        }
    }
}
