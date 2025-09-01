using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Scheduled.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IYSIntegration.Scheduled.Services
{
    public class SingleConsentAddService
    {
        private readonly ILogger<SingleConsentAddService> _logger;
        private readonly IDbHelper _dbHelper;
        private readonly IIntegrationHelper _integrationHelper;

        public SingleConsentAddService(ILogger<SingleConsentAddService> logger, IDbHelper dbHelper, IIntegrationHelper integrationHelper)
        {
            _logger = logger;
            _dbHelper = dbHelper;
            _integrationHelper = integrationHelper;
        }

        public async Task RunAsync(int rowCount)
        {
            bool errorFlag = false;

            _logger.LogInformation("SingleConsentAddService running at: {time}", DateTimeOffset.Now);

            try
            {
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

                        var response = await _integrationHelper.AddConsent(consentRequest);

                        if (response.HttpStatusCode == 0 || response.HttpStatusCode >= 500)
                        {
                            return;
                        }

                        response.Id = log.Id;

                        await _dbHelper.UpdateConsentResponse(response);
                    }
                    catch (Exception ex)
                    {
                        errorFlag = true;
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
        }
    }
}
