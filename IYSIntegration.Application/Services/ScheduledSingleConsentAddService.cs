using IYSIntegration.Application.Interface;
using IYSIntegration.Common.Request.Consent;
using Microsoft.Extensions.Logging;
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

                        Common.Base.ResponseBase<Common.Response.Consent.AddConsentResult> response = new();

                        if (consentRequest.IysCode == 0)
                        {
                            var consentParams = _consentService.GetIysCode(consentRequest.CompanyCode);
                            consentRequest.IysCode = consentParams.IysCode;
                            consentRequest.BrandCode = consentParams.BrandCode;
                        }

                        response = await _consentService.AddConsent(consentRequest);

                        if (!consentRequest.WithoutLogging)
                        {
                            var id = await _dbService.InsertConsentRequest(consentRequest);                       
                            response.Id = id;
                            await _dbService.UpdateConsentResponseFromCommon(response);
                            response.OriginalError = null;
                        }       

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
