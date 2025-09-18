using IYSIntegration.Application.Services.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace IYSIntegration.Application.Services
{
    public class OverdueOldConsentsService : IOverdueOldConsentsService
    {
        private const int DefaultMaxAgeInDays = 3;

        private readonly IDbService _dbService;
        private readonly ILogger<OverdueOldConsentsService> _logger;

        public OverdueOldConsentsService(IDbService dbService, ILogger<OverdueOldConsentsService> logger)
        {
            _dbService = dbService;
            _logger = logger;
        }

        public async Task<int> MarkOverdueAsync()
        {
            try
            {
                var affected = await _dbService.MarkConsentsOverdue(DefaultMaxAgeInDays);
                if (affected > 0)
                {
                    _logger.LogInformation("Marked {Count} consent records as overdue due to age.", affected);
                }

                return affected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark overdue consents.");
                return 0;
            }
        }
    }
}
