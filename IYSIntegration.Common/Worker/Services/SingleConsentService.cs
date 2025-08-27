using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Common.Worker.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        public async Task<ResponseBase<ProcessResult>> ProcessAsync()
        {
            var response = new ResponseBase<ProcessResult>();
            bool errorFlag = false;
            int successCount = 0;
            int failedCount = 0;

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

                        var serviceResponse = await _integrationHelper.AddConsent(consentRequest);

                        if (serviceResponse.HttpStatusCode == 0 || serviceResponse.HttpStatusCode >= 500)
                        {
                            return; // servis hatası durumunda işlemi durdur
                        }

                        serviceResponse.Id = log.Id;

                        await _dbHelper.UpdateConsentResponse(serviceResponse);

                        if (serviceResponse.IsSuccessful())
                            Interlocked.Increment(ref successCount);
                        else
                            Interlocked.Increment(ref failedCount);
                    }
                    catch (Exception ex)
                    {
                        errorFlag = true;
                        Interlocked.Increment(ref failedCount);
                        _logger.LogError("SingleConsentService ID {Id} ve {Message} ile alınamadı", log.Id, ex.Message);
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                errorFlag = true;
                _logger.LogError("SingleConsentService Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
            }

            response.Data = new ProcessResult { SuccessCount = successCount, FailedCount = failedCount };

            if (errorFlag || failedCount > 0)
            {
                response.Error("FailedCount", failedCount.ToString());
            }

            return response;
        }
    }
}
