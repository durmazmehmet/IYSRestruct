using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Application.Models;
using IYSIntegration.Application.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IYSIntegration.Application.Services
{
    public class PullConsentService
    {
        private readonly ILogger<PullConsentService> _logger;
        private readonly IDbHelper _dbHelper;
        private readonly IConfiguration _configuration;
        private readonly IIntegrationHelper _integrationHelper;

        public PullConsentService(IConfiguration configuration, ILogger<PullConsentService> logger, IDbHelper dbHelper, IIntegrationHelper integrationHelper)
        {
            _configuration = configuration;
            _logger = logger;
            _dbHelper = dbHelper;
            _integrationHelper = integrationHelper;
        }

        public async Task RunAsync(int limit)
        {
            bool errorFlag = false;
            List<string> failedCompanyCodes = new List<string>();
            string companyCodeInProc;
            try
            {
                _logger.LogInformation("PullConsentService running at: {time}", DateTimeOffset.Now);
                var companyList = _configuration.GetSection("CompanyCodes").Get<List<string>>() ?? new List<string>();
                foreach (var companyCode in companyList)
                {
                    companyCodeInProc = companyCode;
                    try
                    {
                        var fetchNext = true;
                        while (fetchNext)
                        {
                            await Task.Delay(5000);
                            _logger.LogInformation($"PullConsentService running for: {companyCode}");

                            int iysCode = _configuration.GetValue<int>($"{companyCode}:IysCode");
                            int brandCode = _configuration.GetValue<int>($"{companyCode}:BrandCode");

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
                    catch
                    {
                        errorFlag = true;
                        failedCompanyCodes.Add(companyCodeInProc);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("PullConsentService Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
            }

            if (errorFlag)
            {
                _logger.LogError($"PullConsentService {string.Join(",", failedCompanyCodes)} firmaları için hata aldı");
            }
        }
    }
}
