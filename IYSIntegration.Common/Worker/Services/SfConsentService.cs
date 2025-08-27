using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Common.Worker.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IYSIntegration.Common.Worker.Services
{
    public class SfConsentService
    {
        private readonly ILogger<SfConsentService> _logger;
        private readonly IWorkerDbHelper _dbHelper;
        private readonly IConfiguration _configuration;
        private readonly IIntegrationHelper _integrationHelper;

        public SfConsentService(IConfiguration configuration, ILogger<SfConsentService> logger, IWorkerDbHelper dbHelper, IIntegrationHelper integrationHelper)
        {
            _configuration = configuration;
            _logger = logger;
            _dbHelper = dbHelper;
            _integrationHelper = integrationHelper;
        }

        public async Task<WorkerResult> ProcessAsync()
        {
            var workerResult = new WorkerResult();
            _logger.LogInformation("SfConsentService running at: {time}", DateTimeOffset.Now);
            var rowCount = _configuration.GetValue<int>("SfConsentProcessRowCount");

            var consentRequests = await _dbHelper.GetPullConsentRequests(false, rowCount);
            if (consentRequests?.Count > 0)
            {
                _logger.LogInformation("SfConsentService running at: {count} records processing", consentRequests.Count);
                foreach (var consent in consentRequests)
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

                        var addConsentResult = await _integrationHelper.SfAddConsent(new SfConsentAddRequest { Request = request });
                        if (!string.IsNullOrEmpty(addConsentResult?.WsStatus))
                        {
                            var result = new SfConsentResult
                            {
                                Id = consent.Id,
                                IsSuccess = addConsentResult.WsStatus == "OK",
                                LogId = addConsentResult.LogId,
                                Error = (addConsentResult.WsStatus != "OK") ? addConsentResult.WsDescription : null
                            };

                            await _dbHelper.UpdateSfConsentResponse(result);

                            if (result.IsSuccess)
                                workerResult.SuccessCount++;
                            else
                            {
                                workerResult.FailedCount++;
                                if (!string.IsNullOrEmpty(result.Error))
                                    workerResult.Errors.Add(result.Error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        workerResult.FailedCount++;
                        workerResult.Errors.Add(ex.Message);
                        _logger.LogError("SfConsentService Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
                    }
                }
            }

            return workerResult;
        }
    }
}
