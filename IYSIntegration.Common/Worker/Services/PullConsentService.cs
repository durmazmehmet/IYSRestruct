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
    public class PullConsentService
    {
        private readonly ILogger<PullConsentService> _logger;
        private readonly IWorkerDbHelper _dbHelper;
        private readonly IConfiguration _configuration;
        private readonly IIntegrationHelper _integrationHelper;

        public PullConsentService(IConfiguration configuration, ILogger<PullConsentService> logger, IWorkerDbHelper dbHelper, IIntegrationHelper integrationHelper)
        {
            _configuration = configuration;
            _logger = logger;
            _dbHelper = dbHelper;
            _integrationHelper = integrationHelper;
        }

        public async Task<ResponseBase<ProcessResult>> ProcessAsync()
        {
            var response = new ResponseBase<ProcessResult>();
            bool errorFlag = false;
            List<string> failedCompanyCodes = new();
            int successCount = 0;
            string companyCodeInProc = string.Empty;
            try
            {
                _logger.LogInformation("PullConsentService running at: {time}", DateTimeOffset.Now);
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
                            _logger.LogInformation("PullConsentService running for: {company}", companyCode);

                            int iysCode = _configuration.GetValue<int>($"{companyCode}:IysCode");
                            int brandCode = _configuration.GetValue<int>($"{companyCode}:BrandCode");
                            int limit = _configuration.GetValue<int>("PullConsentBatchSize");

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
                                    successCount++;
                                }

                                await _dbHelper.UpdatePullRequestLog(new PullRequestLog
                                {
                                    CompanyCode = companyCode,
                                    IysCode = pullConsentRequest.IysCode,
                                    BrandCode = pullConsentRequest.BrandCode,
                                    AfterId = pullConsentResult.Data.After
                                });

                                fetchNext = consentList.Length >= limit;
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
                    catch (Exception)
                    {
                        errorFlag = true;
                        failedCompanyCodes.Add(companyCodeInProc);
                    }
                }
            }
            catch (Exception ex)
            {
                errorFlag = true;
                _logger.LogError("PullConsentService Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
            }

            response.Data = new ProcessResult { SuccessCount = successCount, FailedCount = failedCompanyCodes.Count };

            if (failedCompanyCodes.Count > 0)
            {
                response.AddMessage("FailedCompanies", string.Join(",", failedCompanyCodes));
                response.Error("FailedCount", failedCompanyCodes.Count.ToString());
            }

            if (errorFlag)
            {
                // already marked with Error above if failedCompanyCodes > 0, but ensure status is error
                if (failedCompanyCodes.Count == 0)
                    response.Error("Error", "Unknown error");
            }

            return response;
        }
    }
}
