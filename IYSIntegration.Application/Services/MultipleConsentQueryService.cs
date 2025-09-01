using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Application.Models;
using IYSIntegration.Application.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace IYSIntegration.Application.Services
{
    public class MultipleConsentQueryService
    {
        private readonly ILogger<MultipleConsentQueryService> _logger;
        private readonly IDbHelper _dbHelper;
        private readonly IIntegrationHelper _integrationHelper;

        public MultipleConsentQueryService(ILogger<MultipleConsentQueryService> logger, IDbHelper dbHelper, IIntegrationHelper integrationHelper)
        {
            _logger = logger;
            _dbHelper = dbHelper;
            _integrationHelper = integrationHelper;
        }

        public async Task RunAsync(int batchCount)
        {
            bool errorFlag = false;
            int successCount = 0;
            int failedCount = 0;

            try
            {
                _logger.LogInformation("MultipleConsentQueryService running at: {time}", DateTimeOffset.Now);
                var batchList = await _dbHelper.GetUnprocessedMultipleConsenBatches(batchCount);
                var queue = new ConcurrentQueue<BatchConsentQuery>(batchList);
                var options = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };

                Parallel.ForEach(queue, options, q =>
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

                        var result = _integrationHelper.QueryMultipleConsent(queryMultipleConsentRequest).Result;
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

                                    _dbHelper.UpdateMultipleConsentItem(batchItemResult).Wait();
                                }

                                _dbHelper.UpdateMultipleConsentQueryDate(batch.BatchId, result.LogId).Wait();
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
        }
    }
}
