using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Base;
using IYSIntegration.Application.Request.Consent;
using IYSIntegration.Application.Response.Consent;
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
        private readonly IRestClientService _clientHelper;
        private readonly string _baseProxyUrl;

        public ScheduledPullConsentService(IConfiguration configuration, ILogger<ScheduledPullConsentService> logger, IDbService dbHelper, IRestClientService clientHelper)
        {
            _configuration = configuration;
            _logger = logger;
            _dbService = dbHelper;
            _clientHelper = clientHelper;
            _baseProxyUrl = _configuration.GetValue<string>("BaseProxyUrl");
        }

        public async Task<ResponseBase<ScheduledJobStatistics>> RunAsync(int limit)
        {
            var response = new ResponseBase<ScheduledJobStatistics>();
            var results = new ConcurrentBag<LogResult>();
            List<string> failedCompanyCodes = [];
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
                        var consentParams = GetIysCode(companyCode);

                        var fetchNext = true;

                        while (fetchNext)
                        {
                            //await Task.Delay(5000);
                            _logger.LogInformation($"PullConsentService running for: {companyCode}");

                            var pullRequestLog = await _dbService.GetPullRequestLog(companyCode);

                            var proxyRequest = new IysRequest<Consent>
                            {
                                Url = $"{_baseProxyUrl}/{companyCode}?after={pullRequestLog?.AfterId}&limit={limit}&source=IYS",
                                Action = "Add Consent",
                                Method = RestSharp.Method.Get
                            };

                            var pullConsentResult = await _clientHelper.Execute<PullConsentResult, Consent>(proxyRequest);

                            if (pullConsentResult.HttpStatusCode == 0 || pullConsentResult.HttpStatusCode >= 500)
                            {
                                results.Add(new LogResult { Id = 0, Status = "Failed", Message = $"IYS error {pullConsentResult.HttpStatusCode}" });
                                Interlocked.Increment(ref failedCount);
                                _logger.LogError("pullconsent failed (status: {Status}) for company {companyCode}", pullConsentResult.HttpStatusCode, companyCode);
                                continue;
                            }

                            var consentList = pullConsentResult.Data?.List;

                            if (consentList?.Length > 0)
                            {
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

                                await _dbService.UpdateJustRequestDateOfPullRequestLog(new PullRequestLog
                                {
                                    CompanyCode = companyCode,
                                    IysCode = consentParams.IysCode,
                                    BrandCode = consentParams.BrandCode
                                });

                                fetchNext = false;
                            }
                        }

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        results.Add(new LogResult { Id = 0, CompanyCode = companyCode, Status = "Exception", Message = ex.Message });
                        failedCompanyCodes.Add(companyCode);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("PullConsentService Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
            }

            response.Data = new ScheduledJobStatistics
            {
                SuccessCount = successCount,
                FailedCount = failedCompanyCodes.Count,
                FailedCompanyCodes = failedCompanyCodes
            };

            foreach (var result in results)
            {
                var msgKey = $"Consent_{result.Id}_{result.CompanyCode}";
                var msg = $"{result.Status}{(string.IsNullOrWhiteSpace(result.Message) ? "" : $": {result.Message}")}";
                response.AddMessage(msgKey, msg);
            }

            return response;
        }

        private ConsentParams GetIysCode(string companyCode)
        {
            var iysCode = _configuration.GetValue<int?>($"{companyCode}:IysCode");
            var brandCode = _configuration.GetValue<int?>($"{companyCode}:BrandCode");

            if (iysCode == null || brandCode == null)
                throw new InvalidOperationException($"'{companyCode}' için eirşim bilgisi mevcut değil.");

            return new ConsentParams
            {
                IysCode = iysCode.Value,
                BrandCode = brandCode.Value
            };
        }
    }
}
