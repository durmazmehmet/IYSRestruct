using IYSIntegration.Common.Request.Consent;
using IYSIntegration.WorkerService.Models;
using IYSIntegration.WorkerService.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IYSIntegration.WorkerService.Services
{
    public class PullConsentWorker : BackgroundService
    {
        private readonly ILogger<PullConsentWorker> _logger;
        private readonly IDbHelper _dbHelper;
        private readonly IConfiguration _configuration;
        private readonly IIntegrationHelper _integrationHelper;

        public PullConsentWorker(IConfiguration configuration, ILogger<PullConsentWorker> logger, IDbHelper dbHelper, IIntegrationHelper integrationHelper)
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
                List<string> failedCompanyCodes = new List<string>();
                string companyCodeInProc;
                try
                {
                    _logger.LogInformation("ConsentPullWorker running at: {time}", DateTimeOffset.Now);
                    // TODO: 24 saatte 1 çalýuþacak þekilde kurgulanacak Gece 01:00 sonrasý
                    var companyList = new List<string> { "BOI", "BOP", "BOPK", "BOM" };
                    foreach (var companyCode in companyList)
                    {
                        companyCodeInProc = companyCode;
                        try
                        {
                            var fetchNext = true;
                            while (fetchNext)
                            {
                                await Task.Delay(5000);
                                _logger.LogInformation($"ConsentPullWorker running for: {companyCode}");

                                int iysCode = _configuration.GetValue<int>($"{companyCode}:IysCode");
                                int brandCode = _configuration.GetValue<int>($"{companyCode}:BrandCode");
                                int limit = _configuration.GetValue<int>($"PullConsentBatchSize");


                                var pullRequestLog = await _dbHelper.GetPullRequestLog(companyCode);
                                var pullConsentRequest = new PullConsentRequest
                                {
                                    CompanyCode = companyCode,
                                    IysCode = iysCode,
                                    BrandCode = brandCode,
                                    Source = "IYS",
                                    After = pullRequestLog?.AfterId,
                                    Limit = limit
                                };

                                var pullConsentResult = await _integrationHelper.PullConsent(pullConsentRequest);
                                var consentList = pullConsentResult.Data?.List;

                                if (consentList?.Length > 0)
                                {
                                    foreach (var consent in consentList)
                                    {
                                        var addConsentRequest = new AddConsentRequest
                                        {
                                            CompanyCode = companyCode,
                                            IysCode = pullConsentRequest.IysCode,
                                            BrandCode = pullConsentRequest.BrandCode,
                                            Consent = consent
                                        };
                                        await _dbHelper.InsertPullConsent(addConsentRequest);
                                    }

                                    await _dbHelper.UpdatePullRequestLog(new PullRequestLog
                                    {
                                        CompanyCode = companyCode,
                                        IysCode = pullConsentRequest.IysCode,
                                        BrandCode = pullConsentRequest.BrandCode,
                                        AfterId = pullConsentResult.Data.After
                                    });

                                    fetchNext = consentList?.Length >= limit;
                                }
                                else
                                {
                                    await _dbHelper.UpdateJustRequestDateOfPullRequestLog(new PullRequestLog
                                    {
                                        CompanyCode = companyCode,
                                        IysCode = pullConsentRequest.IysCode,
                                        BrandCode = pullConsentRequest.BrandCode
                                    });
                                    fetchNext = false;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            errorFlag = true;
                            failedCompanyCodes.Add(companyCodeInProc);
                        }
                    }
                    ;
                }
                catch (Exception ex)
                {
                    _logger.LogError("PullConsentWorker Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
                }

                if (errorFlag)
                {
                    _logger.LogError($"PullConsentWorker {string.Join(",", failedCompanyCodes)} firmaları için hata aldı");
                }


                await Task.Delay(_configuration.GetValue<int>("PullConsentQueryDelay"), stoppingToken);
            }
        }
    }
}
