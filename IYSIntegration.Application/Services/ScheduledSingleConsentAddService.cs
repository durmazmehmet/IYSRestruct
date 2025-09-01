using IYSIntegration.Application.Interface;
using IYSIntegration.Application.Models;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using Microsoft.Extensions.Logging;
using System.Threading;
namespace IYSIntegration.Application.Services
{
    public class ScheduledSingleConsentAddService
    {
        private readonly ILogger<ScheduledSingleConsentAddService> _logger;
        private readonly IDbService _dbService;
        private readonly IConsentService _consentService;

        public ScheduledSingleConsentAddService(ILogger<ScheduledSingleConsentAddService> logger, IDbService dbHelper, IConsentService consentService)
        {
            _logger = logger;
            _dbService = dbHelper;
            _consentService = consentService;
        }

        public async Task<ResponseBase<ScheduledJobStatistics>> RunAsync(int rowCount)
        {
            var response = new ResponseBase<ScheduledJobStatistics>();
            bool errorFlag = false;
            int failedCount = 0;
            int successCount = 0;

            _logger.LogInformation("SingleConsentAddService running at: {time}", DateTimeOffset.Now);

            try
            {
                var consentRequestLogs = await _dbService.GetConsentRequests(false, rowCount);

                var tasks = consentRequestLogs.Select(async log =>
                {
                    try
                    {
                        var consentRequest = new AddConsentRequest
                        {
                            WithoutLogging = true,
                            IysCode = log.IysCode,
                            BrandCode = log.BrandCode,
                            Consent = new Common.Base.Consent
                            {
                                ConsentDate = log.ConsentDate,
                                Recipient = log.Recipient,
                                RecipientType = log.RecipientType,
                                Source = log.Source,
                                Status = log.Status,
                                Type = log.Type
                            }
                        };

                        var addResponse = new Common.Base.ResponseBase<Common.Response.Consent.AddConsentResult>();

                        if (consentRequest.IysCode == 0)
                        {
                            var consentParams = _consentService.GetIysCode(consentRequest.CompanyCode);
                            consentRequest.IysCode = consentParams.IysCode;
                            consentRequest.BrandCode = consentParams.BrandCode;
                        }

                        addResponse = await _consentService.AddConsent(consentRequest);

                        if (!consentRequest.WithoutLogging)
                        {
                            var id = await _dbService.InsertConsentRequest(consentRequest);
                            addResponse.Id = id;
                            await _dbService.UpdateConsentResponseFromCommon(addResponse);
                            addResponse.OriginalError = null;
                        }

                        if (addResponse.HttpStatusCode == 0 || addResponse.HttpStatusCode >= 500)
                        {
                            Interlocked.Increment(ref failedCount);
                            return;
                        }

                        addResponse.Id = log.Id;

                        await _dbService.UpdateConsentResponse(addResponse);
                        Interlocked.Increment(ref successCount);
                    }
                    catch (Exception ex)
                    {
                        errorFlag = true;
                        Interlocked.Increment(ref failedCount);
                        _logger.LogError($"SingleConsentAddService ID {log.Id} ve {ex.Message} ile alınamadı");
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError("SingleConsentAddService Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
            }

            if (errorFlag)
            {
                _logger.LogError("SingleConsentAddService hata aldı, IYSConsentRequest tablosuna gözatın");
            }

            response.Data = new ScheduledJobStatistics { SuccessCount = successCount, FailedCount = failedCount };
            if (errorFlag || failedCount > 0)
            {
                response.Error("SINGLE_CONSENT_ADD", "Some consents failed to add.");
            }
            return response;
        }
    }
}
