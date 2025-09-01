using IYSIntegration.Application.Interface;
using IYSIntegration.Application.Models;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace IYSIntegration.Application.Services
{
    public class ScheduledMultipleConsentQueryService
    {
        private readonly ILogger<ScheduledMultipleConsentQueryService> _logger;
        private readonly IDbService _dbService;
        private readonly IConsentService _consentService;

        public ScheduledMultipleConsentQueryService(ILogger<ScheduledMultipleConsentQueryService> logger, IDbService dbHelper, IConsentService consentService)
        {
            _logger = logger;
            _dbService = dbHelper;
            _consentService = consentService;
        }

        public async Task<ResponseBase<ScheduledJobStatistics>> RunAsync(int batchCount)
        {
            var response = new ResponseBase<ScheduledJobStatistics>();
            bool errorFlag = false;
            int successCount = 0;
            int failedCount = 0;

            try
            {
                _logger.LogInformation("MultipleConsentQueryService running at: {time}", DateTimeOffset.Now);
                var batchList = await _dbService.GetUnprocessedMultipleConsenBatches(batchCount);
                var queue = new ConcurrentQueue<BatchConsentQuery>(batchList);
                var options = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };

                Parallel.ForEach(queue, options, async q =>
                {
                    try
                    {
                        var batch = new BatchConsentQuery();
                        if (!queue.TryDequeue(out batch))
                        {
                            return;
                        }

                        var queryMultipleConsentRequest = new QueryMultipleConsentRequest
                        {
                            IysCode = batch.IysCode,
                            BrandCode = batch.BrandCode,
                            RequestId = batch.RequestId,
                            BatchId = batch.BatchId
                        };

                        if (queryMultipleConsentRequest.IysCode == 0)
                        {
                            var consentParams = _consentService.GetIysCode(queryMultipleConsentRequest.CompanyCode);
                            queryMultipleConsentRequest.IysCode = consentParams.IysCode;
                            queryMultipleConsentRequest.BrandCode = consentParams.BrandCode;
                        }

                        var result = await _consentService.QueryMultipleConsent(queryMultipleConsentRequest);

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

                                    _dbService.UpdateMultipleConsentItem(batchItemResult).Wait();
                                }

                                _dbService.UpdateMultipleConsentQueryDate(batch.BatchId, result.LogId).Wait();
                            }
                            successCount++;
                        }
                    }
                    catch
                    {
                        errorFlag = true;
                        failedCount++;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("MultipleConsentQueryService Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
            }

            if (errorFlag)
                _logger.LogError($"MultipleConsentQueryService toplam {failedCount + successCount} içinden {failedCount} recipient için hata aldı, IYSConsentRequest tablosuna bakın");

            response.Data = new ScheduledJobStatistics { SuccessCount = successCount, FailedCount = failedCount };
            if (errorFlag)
            {
                response.Error("MULTIPLE_CONSENT_QUERY", "One or more batches failed.");
            }
            return response;
        }
    }
}
