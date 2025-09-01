using IYSIntegration.Application.Interface;
using IYSIntegration.Application.Models;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using Microsoft.Extensions.Logging;

namespace IYSIntegration.Application.Services
{
    public class SfConsentScheduledService
    {
        private readonly ILogger<SfConsentScheduledService> _logger;
        private readonly IDbHelper _dbHelper;
        private readonly IIntegrationHelper _integrationHelper;

        public SfConsentScheduledService(ILogger<SfConsentScheduledService> logger, IDbHelper dbHelper, IIntegrationHelper integrationHelper)
        {
            _logger = logger;
            _dbHelper = dbHelper;
            _integrationHelper = integrationHelper;
        }

        public async Task RunAsync(int rowCount)
        {
            _logger.LogInformation("SfConsentService running at: {time}", DateTimeOffset.Now);
            var consentRequests = await _dbHelper.GetPullConsentRequests(false, rowCount);
            if (consentRequests?.Count > 0)
            {
                var latestConsents = consentRequests
                    .GroupBy(x => new { x.CompanyCode, x.Recipient })
                    .Select(g => g.OrderByDescending(x => x.CreateDate).First())
                    .ToList();

                var outdatedConsents = consentRequests
                    .GroupBy(x => new { x.CompanyCode, x.Recipient })
                    .SelectMany(g => g.OrderByDescending(x => x.CreateDate).Skip(1))
                    .ToList();

                foreach (var outdated in outdatedConsents)
                {
                    try
                    {
                        var skipResult = new SfConsentResult
                        {
                            Id = outdated.Id,
                            IsSuccess = false,
                            LogId = 0,
                            Error = "Superseded by newer consent",
                        };
                        _dbHelper.UpdateSfConsentResponse(skipResult).Wait();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("SfConsentService duplicate skip error: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
                    }
                }

                _logger.LogInformation($"SfConsentService running at: {latestConsents?.Count} records processing");
                foreach (var consent in latestConsents)
                {
                    try
                    {
                        var request = new SfConsentBase
                        {
                            CompanyCode = consent.CompanyCode
                        };

                        if (consent.Type != "EPOSTA" && consent.Recipient.StartsWith("+90"))
                            consent.Recipient = consent.Recipient.Substring(3);

                        consent.CreationDate = null;
                        consent.TransactionId = null;
                        consent.CompanyCode = null;
                        request.Consents = new List<Consent>
                        {
                            consent
                        };

                        var addConsentResult = _integrationHelper.SfAddConsent(new SfConsentAddRequest { Request = request }).Result;
                        if (!string.IsNullOrEmpty(addConsentResult?.WsStatus))
                        {
                            var result = new SfConsentResult
                            {
                                Id = consent.Id,
                                IsSuccess = addConsentResult.WsStatus == "OK",
                                LogId = addConsentResult.LogId,
                                Error = (addConsentResult.WsStatus != "OK") ? addConsentResult.WsDescription : null
                            };

                            _dbHelper.UpdateSfConsentResponse(result).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("SfConsentService Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
                    }
                }
            }
        }
    }
}
