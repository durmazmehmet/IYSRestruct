using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IYSIntegration.Application.Services
{
    public class DuplicateCleanerService : IDuplicateCleanerService
    {
        private readonly IDbService _dbService;
        private readonly ScheduledDuplicateConsentCleanupService _scheduledCleanupService;
        private readonly ILogger<DuplicateCleanerService> _logger;

        public DuplicateCleanerService(
            IDbService dbService,
            ScheduledDuplicateConsentCleanupService scheduledCleanupService,
            ILogger<DuplicateCleanerService> logger)
        {
            _dbService = dbService;
            _scheduledCleanupService = scheduledCleanupService;
            _logger = logger;
        }

        public async Task CleanAsync(IEnumerable<Consent> consents)
        {
            if (consents == null)
            {
                return;
            }

            var consentList = consents
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.CompanyCode) && !string.IsNullOrWhiteSpace(c.Recipient))
                .ToList();

            if (consentList.Count == 0)
            {
                return;
            }

            try
            {
                await _dbService.MarkDuplicateConsentsOverdueForConsents(consentList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DuplicateCleanerService failed while marking duplicates.");
            }
        }

        public Task<ResponseBase<ScheduledJobStatistics>> RunBatchAsync()
        {
            return _scheduledCleanupService.RunAsync();
        }
    }
}
