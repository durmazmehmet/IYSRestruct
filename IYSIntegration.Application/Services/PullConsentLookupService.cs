using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Response.Consent;
using Microsoft.Extensions.Logging;

namespace IYSIntegration.Application.Services
{
    public class PullConsentLookupService
    {
        private readonly ILogger<PullConsentLookupService> _logger;
        private readonly IDbService _dbService;

        public PullConsentLookupService(ILogger<PullConsentLookupService> logger, IDbService dbService)
        {
            _logger = logger;
            _dbService = dbService;
        }

        public async Task<ResponseBase<List<PullConsentSummary>>> BodCommercialConsents(int dayCount)
        {
            string[] BodTargetCompanyCodes = new[] { "BAI", "BOD" };
            string BodTargetRecipientType = "TACIR";
            var response = new ResponseBase<List<PullConsentSummary>>();

            if (dayCount <= 0)
            {
                response.Error("dayCount", "Gün parametresi 0'dan büyük olmalıdır.");
                return response;
            }

            try
            {
                _logger.LogInformation("PullConsentLookupService.BodCommercialConsents running at: {time} for last {dayCount} day(s)", DateTimeOffset.Now, dayCount);

                var startDate = DateTime.Now.AddDays(-dayCount);
                var consents = await _dbService.GetPullConsentsAsync(startDate, BodTargetRecipientType, BodTargetCompanyCodes);

                response.Success(consents ?? new List<PullConsentSummary>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PullConsentLookupService.BodCommercialConsents error for last {dayCount} day(s)", dayCount);
                response.Error("Exception", ex.Message);
            }

            return response;
        }
    }
}
