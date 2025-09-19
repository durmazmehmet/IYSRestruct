using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request.Consent;
using IYSIntegration.Application.Services.Models.Response.Consent;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace IYSIntegration.Application.Services
{
    public class PullConsentFromIysService
    {
        private readonly ILogger<PullConsentFromIysService> _logger;
        private readonly IDbService _dbService;
        private readonly IysProxy _client;
        private readonly IIysHelper _iysHelper;

        public PullConsentFromIysService(ILogger<PullConsentFromIysService> logger, IDbService dbHelper, IIysHelper iysHelper, IysProxy client)
        {
            _logger = logger;
            _dbService = dbHelper;
            _client = client;
            _iysHelper = iysHelper;
        }

        public async Task<ResponseBase<ScheduledJobStatistics>> RunAsync(int limit = 999, bool resetAfter = false)
        {
            var response = new ResponseBase<ScheduledJobStatistics>();
            response.Success();
            var results = new ConcurrentBag<LogResult>();
            int failedCount = 0;
            int successCount = 0;

            try
            {
                _logger.LogInformation("PullConsentService running at: {time}", DateTimeOffset.Now);

                var companyList = _iysHelper.GetAllCompanyCodes();

                foreach (var companyCode in companyList)
                {
                    try
                    {
                        var consentParams = _iysHelper.GetIysCode(companyCode);

                        //await Task.Delay(5000);

                        _logger.LogInformation($"PullConsentService running for: {companyCode}");

                        var pullRequestLog = resetAfter ? new PullRequestLog() : await _dbService.GetPullRequestLog(companyCode);
                        var queryParams = new Dictionary<string, string?>
                        {
                            ["after"] = pullRequestLog?.AfterId,
                            ["limit"] = limit.ToString(),
                            ["source"] = "IYS"
                        };

                        var pullConsentResult = await _client.GetAsync<PullConsentResult>(
                            $"consents/{companyCode}/pullConsent",
                            queryParams);

                        if (!pullConsentResult.IsSuccessful() || pullConsentResult.HttpStatusCode == 0 || pullConsentResult.HttpStatusCode >= 500)
                        {
                            results.Add(new LogResult { Id = 0, CompanyCode = companyCode, Messages = pullConsentResult.Messages });
                            Interlocked.Increment(ref failedCount);
                            _logger.LogError("pullconsent failed (status: {Status}) for company {companyCode}", pullConsentResult.HttpStatusCode, companyCode);
                            response.Error();
                            continue;
                        }

                        var consentList = pullConsentResult.Data?.List;

                        if (consentList?.Length > 0)
                        {
                            results.Add(new LogResult { Id = 0, CompanyCode = companyCode, Messages = new Dictionary<string, string> { { "Info", consentList?.Length.ToString() ?? "0" } } });
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
                        }
                        else
                        {
                            results.Add(new LogResult { Id = 0, CompanyCode = companyCode, Messages = new Dictionary<string, string> { { "Info", consentList?.Length.ToString() ?? "0" } } });
                            await _dbService.UpdateJustRequestDateOfPullRequestLog(new PullRequestLog
                            {
                                CompanyCode = companyCode,
                                IysCode = consentParams.IysCode,
                                BrandCode = consentParams.BrandCode
                            });
                        }

                        Interlocked.Increment(ref successCount);
                    }
                    catch (Exception ex)
                    {
                        results.Add(new LogResult { Id = 0, CompanyCode = companyCode, Messages = new Dictionary<string, string> { { "Exception", ex.Message } } });
                        Interlocked.Increment(ref failedCount);
                        response.Error();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("PullConsentService Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
                results.Add(new LogResult { Id = 0, CompanyCode = "", Messages = new Dictionary<string, string> { { "Exception", ex.Message } } });
                response.Error();
            }

            foreach (var result in results)
            {
                response.AddMessage(result.GetMessages());
            }
            response.Data = new ScheduledJobStatistics
            {
                SuccessCount = successCount,
                FailedCount = failedCount
            };
            return response;
        }
    }
}
