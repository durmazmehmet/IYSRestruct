using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Response.Consent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;
namespace IYSIntegration.Application.Services
{
    public class ScheduledMultipleConsentQueryService
    {
        private readonly ILogger<ScheduledMultipleConsentQueryService> _logger;
        private readonly IDbService _dbService;
        private readonly IysProxy _client;
        private readonly IIysHelper _iysHelper;

        public ScheduledMultipleConsentQueryService(ILogger<ScheduledMultipleConsentQueryService> logger, IDbService dbHelper, IIysHelper iysHelper, IConfiguration config)
        {
            _logger = logger;
            _dbService = dbHelper;
            _client = new IysProxy(config.GetValue<string>("BaseIysProxyUrl"));
            _iysHelper = iysHelper;
        }

        public async Task<ResponseBase<ScheduledJobStatistics>> RunAsync(int batchCount)
        {
            var response = new ResponseBase<ScheduledJobStatistics>();
            var results = new ConcurrentBag<LogResult>();
            int successCount = 0;
            int failedCount = 0;

            try
            {
                _logger.LogInformation("MultipleConsentQueryService running at: {time}", DateTimeOffset.Now);
                var batchList = await _dbService.GetUnprocessedMultipleConsenBatches(batchCount);
                var options = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };

                await Parallel.ForEachAsync(batchList, options, async (batch, _) =>
                {
                    var companyCode = _iysHelper.GetCompanyCode(batch.IysCode);

                    try
                    {
                        var queryParams = new Dictionary<string, string?>
                        {
                            ["requestId"] = batch.RequestId,
                            ["batchId"] = batch.BatchId.ToString()
                        };

                        var result = await _client.GetAsync<List<QueryMultipleConsentResult>>($"consents/{companyCode}/queryMultipleConsent", queryParams);

                        if (result.IsSuccessful())
                        {
                            if (!result.Data.Any(p => p.Status == "enqueue"))
                            {
                                foreach (var item in result.Data.Where(p => p.Status != "success"))
                                {
                                    var batchItemResult = new BatchItemResult
                                    {
                                        BatchId = batch.BatchId,
                                        Index = item.Index + 1,
                                        IsSuccess = false,
                                        BatchError = JsonConvert.SerializeObject(item.Error, Formatting.None, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }),
                                        IsQueryResult = true
                                    };

                                    await _dbService.UpdateMultipleConsentItem(batchItemResult);
                                }

                                await _dbService.UpdateMultipleConsentQueryDate(batch.BatchId, result.LogId);
                            }
                            Interlocked.Increment(ref successCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        results.Add(new LogResult { Id = batch.BatchId, CompanyCode = companyCode, Messages = new Dictionary<string, string> { { "Exception", ex.Message } } });
                        Interlocked.Increment(ref failedCount);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("MultipleConsentQueryService Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
                results.Add(new LogResult { Id = 0, CompanyCode = "", Messages = new Dictionary<string, string> { { "Exception", ex.Message } } });
            }

            response.Data = new ScheduledJobStatistics { SuccessCount = successCount, FailedCount = failedCount };

            return response;
        }
    }
}
