using IYSIntegration.Application.Base;
using IYSIntegration.Application.Request.Consent;
using IYSIntegration.Application.Response.Consent;
using IYSIntegration.Application.Services.Helpers;
using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace IYSIntegration.Application.Services
{
    public class ScheduledPullConsentService
    {
        private readonly ILogger<ScheduledPullConsentService> _logger;
        private readonly IDbService _dbService;
        private readonly IConfiguration _configuration;
        private readonly IysClient _client;
        private readonly IIysHelper _iysHelper;

        public ScheduledPullConsentService(IConfiguration configuration, ILogger<ScheduledPullConsentService> logger, IDbService dbHelper, IIysHelper iysHelper)
        {
            _configuration = configuration;
            _logger = logger;
            _dbService = dbHelper;
            _client = new IysClient(_configuration);
            _iysHelper = iysHelper;
        }

        public async Task<ResponseBase<ScheduledJobStatistics>> RunAsync(int limit)
        {
            var response = new ResponseBase<ScheduledJobStatistics>();
            var results = new ConcurrentBag<LogResult>();
            int failedCount = 0;
            int successCount = 0;

            try
            {
                _logger.LogInformation("PullConsentService running at: {time}", DateTimeOffset.Now);

                var companyList = _configuration.GetSection("CompanyCodes").Get<List<string>>() ?? [];

                foreach (var companyCode in companyList)
                {
                    try
                    {
                        var consentParams = _iysHelper.GetIysCode(companyCode);

                        var fetchNext = true;

                        while (fetchNext)
                        {
                            //await Task.Delay(5000);
                            _logger.LogInformation($"PullConsentService running for: {companyCode}");

                            var pullRequestLog = await _dbService.GetPullRequestLog(companyCode);


                            var queryParams = new Dictionary<string, string?>
                            {
                                ["after"] = pullRequestLog?.AfterId,
                                ["limit"] = limit.ToString(),
                                ["source"] = "IYS"
                            };

                            var pullConsentResult = await _client.GetAsync<PullConsentResult>(
                                $"{companyCode}/pullConsent",
                                queryParams);

                            if (!pullConsentResult.IsSuccessful() || pullConsentResult.HttpStatusCode == 0 || pullConsentResult.HttpStatusCode >= 500)
                            {
                                results.Add(new LogResult { Id = 0,CompanyCode = companyCode, Status = "Failed", Message = $"IYS error {pullConsentResult.OriginalError}" });
                                Interlocked.Increment(ref failedCount);
                                _logger.LogError("pullconsent failed (status: {Status}) for company {companyCode}", pullConsentResult.HttpStatusCode, companyCode);
                                continue;
                            }

                            var consentList = pullConsentResult.Data?.List;
                            
                            if (consentList?.Length > 0)
                            {
                                results.Add(new LogResult { Id = 0, CompanyCode = companyCode, Status = "Recordcount", Message = consentList?.Length.ToString() });
                                foreach (var consent in consentList)
                                {
                                    var addConsentRequest = new AddConsentRequest
                                    {
                                        CompanyCode = companyCode,
                                        IysCode = consentParams.IysCode,
                                        BrandCode = consentParams.BrandCode,
                                        Consent = consent
                                    };
                                    await _dbService.InsertPullConsent(addConsentRequest);
                                }

                                await _dbService.UpdatePullRequestLog(new PullRequestLog
                                {
                                    CompanyCode = companyCode,
                                    IysCode = consentParams.IysCode,
                                    BrandCode = consentParams.BrandCode,
                                    AfterId = pullConsentResult.Data.After
                                });

                                fetchNext = consentList?.Length >= limit;
                            }
                            else
                            {
                                results.Add(new LogResult { Id = 0, CompanyCode = companyCode, Status = "Recordcount", Message = "0"});
                                await _dbService.UpdateJustRequestDateOfPullRequestLog(new PullRequestLog
                                {
                                    CompanyCode = companyCode,
                                    IysCode = consentParams.IysCode,
                                    BrandCode = consentParams.BrandCode
                                });

                                fetchNext = false;
                            }
                        }

                        Interlocked.Increment(ref successCount); 
                    }
                    catch (Exception ex)
                    {
                        results.Add(new LogResult { Id = 0, CompanyCode = companyCode, Status = "Exception", Message = ex.Message });
                        Interlocked.Increment(ref failedCount);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("PullConsentService Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
                results.Add(new LogResult { Id = 0, CompanyCode = "", Status = "Failed", Message = $"Genel hata. {ex.Message}" });
            }

            response.Data = new ScheduledJobStatistics
            {
                SuccessCount = successCount,
                FailedCount = failedCount
            };

            foreach (var result in results)
            {
                var msgKey = result.CompanyCode;
                var msg = $"{result.Status}{(string.IsNullOrWhiteSpace(result.Message) ? "" : $": {result.Message}")}";
                response.AddMessage(msgKey, msg);
            }

            return response;
        }
    }
}
