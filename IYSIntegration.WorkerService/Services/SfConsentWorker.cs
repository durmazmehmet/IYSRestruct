using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.WorkerService.Models;
using IYSIntegration.WorkerService.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IYSIntegration.WorkerService.Services
{
    public class SfConsentWorker : BackgroundService
    {
        private readonly ILogger<SfConsentWorker> _logger;
        private readonly IDbHelper _dbHelper;
        private readonly IConfiguration _configuration;
        private readonly IIntegrationHelper _integrationHelper;

        public SfConsentWorker(IConfiguration configuration, ILogger<SfConsentWorker> logger, IDbHelper dbHelper, IIntegrationHelper integrationHelper)
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
                _logger.LogInformation("SfConsentWorker running at: {time}", DateTimeOffset.Now);
                var rowCount = _configuration.GetValue<int>("SfConsentProcessRowCount");

                //Ard arda gelen kayıtlar SF için sıkıntı oluyor ya list shuffle yapacağız yada tek tek vereceğiz. Tek tek seçtik

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
                                Error = "Superseded by newer consent"
                            };
                            _dbHelper.UpdateSfConsentResponse(skipResult).Wait();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("SfConsentWorker duplicate skip error: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
                        }
                    }

                    _logger.LogInformation($"SfConsentWorker running at: {latestConsents?.Count} records processing");
                    foreach (var consent in latestConsents)
                    {
                        try
                        {
                            var request = new SfConsentBase
                            {
                                CompanyCode = consent.CompanyCode
                            };

                            // TODO: Telefon formatýný Salesforce'a uygun þekilde hazýrlama
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
                            _logger.LogError("SfConsentWorker Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
                        }
                    }

                }
                await Task.Delay(_configuration.GetValue<int>("SfAddConsentDelay"), stoppingToken);
            }
        }
    }
}