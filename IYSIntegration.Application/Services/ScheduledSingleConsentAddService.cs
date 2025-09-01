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

    public class ScheduledSingleConsentAddService
    {
        private readonly ILogger<ScheduledSingleConsentAddService> _logger;
        private readonly IDbService _dbService;
        private readonly IRestClientService _clientHelper;
        private readonly IConfiguration _config;
        private readonly string _baseProxyUrl;

        public ScheduledSingleConsentAddService(IConfiguration config, ILogger<ScheduledSingleConsentAddService> logger, IDbService dbHelper, IRestClientService clientHelper)
        {
            _logger = logger;
            _dbService = dbHelper;
            _clientHelper = clientHelper;
            _baseProxyUrl = _config.GetValue<string>("BaseProxyUrl");
        }

        public async Task<ResponseBase<ScheduledJobStatistics>> RunAsync(int rowCount)
        {
            var response = new ResponseBase<ScheduledJobStatistics>();
            int failedCount = 0;
            int successCount = 0;

            var results = new ConcurrentBag<LogResult>();

            _logger.LogInformation("SingleConsentAddService started at {Time}", DateTimeOffset.Now);

            try
            {
                var logs = await _dbService.GetConsentRequests(false, rowCount);

                var tasks = logs.Select(async log =>
                {
                    try
                    {
                        var request = new AddConsentRequest
                        {
                            WithoutLogging = true,
                            IysCode = log.IysCode,
                            BrandCode = log.BrandCode,
                            Consent = new Application.Base.Consent
                            {
                                ConsentDate = log.ConsentDate,
                                Recipient = log.Recipient,
                                RecipientType = log.RecipientType,
                                Source = log.Source,
                                Status = log.Status,
                                Type = log.Type
                            }
                        };

                        if (!string.IsNullOrWhiteSpace(request.Consent?.ConsentDate) &&
                            DateTime.TryParse(request.Consent.ConsentDate, out var consentDate) &&
                            IsOlderThanBusinessDays(consentDate, 3))
                        {
                            results.Add(new LogResult { Id = log.Id, CompanyCode = request.CompanyCode, Status = "Skipped", Message = "Consent older than 3 business days" });
                            Interlocked.Increment(ref failedCount);
                            _logger.LogWarning("Consent ID {Id} skipped: older than 3 business days", log.Id);
                            return;
                        }

                        var proxyRequest = new IysRequest<Consent>
                        {
                            Url = $"{_baseProxyUrl}/{request.CompanyCode}",
                            Body = request.Consent,
                            Action = "Add Consent",
                            Method = RestSharp.Method.Post
                        };

                        var addResponse = await _clientHelper.Execute<AddConsentResult, Consent>(proxyRequest);

                        if (!request.WithoutLogging)
                        {
                            var id = await _dbService.InsertConsentRequest(request);
                            addResponse.Id = id;
                            await _dbService.UpdateConsentResponseFromCommon(addResponse);
                            addResponse.OriginalError = null;
                        }

                        if (addResponse.HttpStatusCode == 0 || addResponse.HttpStatusCode >= 500)
                        {
                            results.Add(new LogResult { Id = log.Id, Status = "Failed", Message = $"IYS error {addResponse.HttpStatusCode}" });
                            Interlocked.Increment(ref failedCount);
                            _logger.LogError("AddConsent failed (status: {Status}) for log ID {Id}", addResponse.HttpStatusCode, log.Id);
                            return;
                        }

                        addResponse.Id = log.Id;
                        await _dbService.UpdateConsentResponse(addResponse);
                        Interlocked.Increment(ref successCount);
                        results.Add(new LogResult { Id = log.Id, Status = "Success" });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new LogResult { Id = log.Id, Status = "Exception", Message = ex.Message });
                        Interlocked.Increment(ref failedCount);
                        _logger.LogError(ex, "Exception in SingleConsentAddService for log ID {Id}", log.Id);
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in SingleConsentAddService");
                response.Error("SINGLE_CONSENT_ADD_FATAL", "Service failed with an unexpected exception.");
            }

            if (failedCount > 0)
            {
                _logger.LogWarning("SingleConsentAddService completed with {FailedCount} failures", failedCount);
                response.Error("SINGLE_CONSENT_ADD", "Some consents failed to add.");
            }

            response.Data = new ScheduledJobStatistics
            {
                SuccessCount = successCount,
                FailedCount = failedCount
            };

            foreach (var result in results)
            {
                var msgKey = $"Consent_{result.Id}_{result.CompanyCode}";
                var msg = $"{result.Status}{(string.IsNullOrWhiteSpace(result.Message) ? "" : $": {result.Message}")}";
                response.AddMessage(msgKey, msg);
            }

            return response;
        }


        private static bool IsOlderThanBusinessDays(DateTime consentDate, int maxBusinessDays)
        {
            var date = consentDate.Date;
            var today = DateTime.Now.Date;
            int businessDays = 0;

            while (date < today)
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    businessDays++;
                }

                if (businessDays >= maxBusinessDays)
                {
                    return true;
                }

                date = date.AddDays(1);
            }

            return false;
        }
    }
}
