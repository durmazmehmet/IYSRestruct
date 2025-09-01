using IYSIntegration.Application.Interface;
using IYSIntegration.Application.Models;
using IYSIntegration.Common.Request.Consent;
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

        public ScheduledPullConsentService(IConfiguration configuration, ILogger<ScheduledPullConsentService> logger, IDbService dbHelper, IConsentService consentService)
        {
            _configuration = configuration;
            _logger = logger;
            _dbService = dbHelper;
            _consentService = consentService;
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

                            var pullConsentResult = await _consentService.PullConsent(pullConsentRequest);

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
