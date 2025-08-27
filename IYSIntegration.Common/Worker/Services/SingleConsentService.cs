using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using IYSIntegration.Common.Worker.Models;

namespace IYSIntegration.Common.Worker.Services
{
    public class SingleConsentService
    {
        private readonly ILogger<SingleConsentService> _logger;
        private readonly IWorkerDbHelper _dbHelper;
        private readonly IConfiguration _configuration;
        private readonly IIntegrationHelper _integrationHelper;

        public SingleConsentService(IConfiguration configuration, ILogger<SingleConsentService> logger, IWorkerDbHelper dbHelper, IIntegrationHelper integrationHelper)
        {
            _configuration = configuration;
            _logger = logger;
            _dbHelper = dbHelper;
            _integrationHelper = integrationHelper;
        }

        public async Task<WorkerResult> ProcessAsync()
        {
            bool errorFlag = false;
            var successCount = 0;
            var failedCount = 0;
            var errors = new ConcurrentBag<string>();
            _logger.LogInformation("SingleConsentService running at: {time}", DateTimeOffset.Now);

            try
            {
                var rowCount = _configuration.GetValue<int>("SingleConsentProcessRowCount");
                var consentRequestLogs = await _dbHelper.GetConsentRequests(false, rowCount);

                var tasks = consentRequestLogs.Select(async log =>
                {
                    try
                    {
                        var consentRequest = new AddConsentRequest
                        {
                            WithoutLogging = true,
                            IysCode = log.IysCode,
                            BrandCode = log.BrandCode,
                            Consent = new Consent
                            {
                                ConsentDate = log.ConsentDate,
                                Recipient = log.Recipient,
                                RecipientType = log.RecipientType,
                                Source = log.Source,
                                Status = log.Status,
                                Type = log.Type
                            }
                        };

                        var response = await _integrationHelper.AddConsent(consentRequest);

                        if (response.HttpStatusCode == 0 || response.HttpStatusCode >= 500)
                        {
                            // servis hatası durumunda işlemi durdur
                            errors.Add($"ID {log.Id}: service error");
                            Interlocked.Increment(ref failedCount);
                            return;
                        }

                        response.Id = log.Id;

                        await _dbHelper.UpdateConsentResponse(response);
                        Interlocked.Increment(ref successCount);
                    }
                    catch (Exception ex)
                    {
                        errorFlag = true;
                        errors.Add($"ID {log.Id}: {ex.Message}");
                        Interlocked.Increment(ref failedCount);
                        _logger.LogError("SingleConsentService ID {Id} ve {Message} ile alınamadı", log.Id, ex.Message);
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError("SingleConsentService Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
                errors.Add(ex.Message);
            }

            if (errorFlag)
            {
                _logger.LogError("SingleConsentService hata aldı, IYSConsentRequest tablosuna göz atın");
            }

            return new WorkerResult
            {
                SuccessCount = successCount,
                FailedCount = failedCount,
                Errors = errors.ToList()
            };
        }
    }
}
