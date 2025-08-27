using IYSIntegration.Common.Request.Consent;
using IYSIntegration.WorkerService.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IYSIntegration.WorkerService.Services
{
    public class SingleConsentAddWorker : BackgroundService
    {
        private readonly ILogger<SingleConsentAddWorker> _logger;
        private readonly IDbHelper _dbHelper;
        private readonly IConfiguration _configuration;
        private readonly IIntegrationHelper _integrationHelper;

        public SingleConsentAddWorker(IConfiguration configuration, ILogger<SingleConsentAddWorker> logger, IDbHelper dbHelper, IIntegrationHelper integrationHelper)
        {
            _configuration = configuration;
            _logger = logger;
            _dbHelper = dbHelper;
            _integrationHelper = integrationHelper;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                bool errorFlag = false;

                _logger.LogInformation("SingleConsentWorker running at: {time}", DateTimeOffset.Now);

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
                                // Servis devre dışıdır bir sonraki döngüye kadar denemenin anlamı yok
                                return;
                            }

                            response.Id = log.Id;

                            await _dbHelper.UpdateConsentResponse(response);
                        }
                        catch (Exception ex)
                        {
                            errorFlag = true;
                            _logger.LogError($"SingleConsentWorker ID {log.Id} ve {ex.Message} ile alınamadı");
                        }
                    });

                    await Task.WhenAll(tasks); // Tüm görevlerin tamamlanmasını bekler
                }
                catch (Exception ex)
                {
                    _logger.LogError("SingleConsentAddWorker Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
                }

                if (errorFlag)
                {
                    _logger.LogError("SingleConsentAddWorker hata aldı, IYSConsentRequest tablosuna gözatın");
                }

                await Task.Delay(_configuration.GetValue<int>("SingleConsentQueryDelay"), stoppingToken);
            }
        }

    }
}
