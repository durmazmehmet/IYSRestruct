using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request.Consent;
using IYSIntegration.Application.Services.Models.Response.Consent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IYSIntegration.Application.Services
{
    public class ScheduledSfConsentService
    {
        private readonly ILogger<ScheduledSfConsentService> _logger;
        private readonly IDbService _dbService;
        private readonly SfClient _client;

        public ScheduledSfConsentService(ILogger<ScheduledSfConsentService> logger, IDbService dbHelper, SfClient sfClient)
        {
            _logger = logger;
            _dbService = dbHelper;
            _client = sfClient;
        }

        public async Task<ResponseBase<ScheduledJobStatistics>> RunAsync(int rowCount)
        {
            var response = new ResponseBase<ScheduledJobStatistics>();
            int successCount = 0;
            int failedCount = 0;
            bool errorFlag = false;

            _logger.LogInformation("SfConsentService running at: {time}", DateTimeOffset.Now);
            var consentRequests = await _dbService.GetPullConsentRequests(false, rowCount);
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
                        _dbService.UpdateSfConsentResponse(skipResult).Wait();
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

                        var addConsentResult = await _client.PostJsonAsync<SfConsentAddRequest, SfConsentAddResponse>("AddConsent", new SfConsentAddRequest { Request = request });


                        if (addConsentResult.IsSuccessful())
                        {
                            var result = new SfConsentResult
                            {
                                Id = consent.Id,
                                IsSuccess = addConsentResult.IsSuccessful(),
                                LogId = addConsentResult.LogId,
                                Error = (addConsentResult.IsSuccessful()) ? string.Empty : addConsentResult.OriginalError?.Message ?? "Unknown error",
                            };

                            _dbService.UpdateSfConsentResponse(result).Wait();

                            if (result.IsSuccess)
                                successCount++;
                            else
                            {
                                errorFlag = true;
                                failedCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errorFlag = true;
                        failedCount++;
                        _logger.LogError("SfConsentService Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
                    }
                }
            }

            response.Data = new ScheduledJobStatistics { SuccessCount = successCount, FailedCount = failedCount };
            if (errorFlag || failedCount > 0)
            {
                response.Error("SF_CONSENT", "Some consents failed to send to SF.");
            }
            return response;
        }
    }
}
