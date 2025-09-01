using IYSIntegration.Application.Interface;
using IYSIntegration.Common.Request.Consent;
using Microsoft.Extensions.Logging;
namespace IYSIntegration.Application.Services
{
    public class SingleConsentAddService
    {
        private readonly ILogger<SingleConsentAddService> _logger;
        private readonly IDbService _dbService;
        private readonly IIntegrationService _integrationService;

        public SingleConsentAddService(ILogger<SingleConsentAddService> logger, IDbService dbHelper, IIntegrationService integrationHelper)
        {
            _logger = logger;
            _dbService = dbHelper;
            _integrationService = integrationHelper;
        }

        public async Task RunAsync(int rowCount)
        {
            bool errorFlag = false;

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

                        var response = await _integrationService.AddConsent(consentRequest);

                        if (response.HttpStatusCode == 0 || response.HttpStatusCode >= 500)
                        {
                            return;
                        }

                        response.Id = log.Id;

                        await _dbService.UpdateConsentResponse(response);
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
