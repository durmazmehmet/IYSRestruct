using IYSIntegration.Application.Interface;
using IYSIntegration.Application.Models;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Common.Response.Consent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IYSIntegration.Application.Services
{
    public class ScheduledPullConsentService
    {
        private readonly ILogger<ScheduledPullConsentService> _logger;
        private readonly IDbService _dbService;
        private readonly IConfiguration _configuration;
        private readonly IConsentService _consentService;
        private readonly SimpleRestClient _restClient;
        private readonly string _baseProxyUrl;

        public ScheduledPullConsentService(IConfiguration configuration, ILogger<ScheduledPullConsentService> logger, IDbService dbHelper, IConsentService consentService)
        {
            _configuration = configuration;
            _logger = logger;
            _dbService = dbHelper;
            _consentService = consentService;
            _restClient = new SimpleRestClient();
            _baseProxyUrl = _configuration.GetValue<string>("IysProxyBaseUrl");
        }

        public async Task<ResponseBase<ScheduledJobStatistics>> RunAsync(int limit)
        {
            var response = new ResponseBase<ScheduledJobStatistics>();
            bool errorFlag = false;
            List<string> failedCompanyCodes = new List<string>();
            int successCount = 0;
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
                            //await Task.Delay(5000);
                            _logger.LogInformation($"PullConsentService running for: {companyCode}");

                            int iysCode = _configuration.GetValue<int>($"{companyCode}:IysCode");
                            int brandCode = _configuration.GetValue<int>($"{companyCode}:BrandCode");

                            var pullRequestLog = await _dbService.GetPullRequestLog(companyCode);
                            var pullConsentRequest = new PullConsentRequest
                            {
                                CompanyCode = companyCode,
                                IysCode = iysCode,
                                BrandCode = brandCode,
                                Source = "IYS",
                                After = pullRequestLog?.AfterId,
                                Limit = limit
                            };

                            if (pullConsentRequest.IysCode == 0)
                            {
                                var consentParams = _consentService.GetIysCode(pullConsentRequest.CompanyCode);
                                pullConsentRequest.IysCode = consentParams.IysCode;
                                pullConsentRequest.BrandCode = consentParams.BrandCode;
                            }

                            var url = $"{_baseProxyUrl}/{companyCode}/pullConsent?" +
                                      $"after={pullRequestLog?.AfterId}&limit={limit}&source={pullConsentRequest.Source}";

                            var pullConsentResult = await _restClient.GetAsync<PullConsentResult>(url);

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
                                    await _dbService.InsertPullConsent(addConsentRequest);
                                }

                                await _dbService.UpdatePullRequestLog(new PullRequestLog
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
                                await _dbService.UpdateJustRequestDateOfPullRequestLog(new PullRequestLog
                                {
                                    CompanyCode = companyCode,
                                    IysCode = pullConsentRequest.IysCode,
                                    BrandCode = pullConsentRequest.BrandCode
                                });
                                fetchNext = false;
                            }
                        }
                        successCount++;
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

            response.Data = new ScheduledJobStatistics
            {
                SuccessCount = successCount,
                FailedCount = failedCompanyCodes.Count,
                FailedCompanyCodes = failedCompanyCodes
            };
            if (errorFlag)
            {
                response.Error("PULL_CONSENT", "Some companies failed during pull.");
            }
            return response;
        }
    }
}
