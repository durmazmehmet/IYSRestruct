using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.WorkerService.Models;
using IYSIntegration.WorkerService.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IYSIntegration.WorkerService.Services
{
    public class MultipleConsentQueryWorker : BackgroundService
    {
        private readonly ILogger<MultipleConsentQueryWorker> _logger;
        private readonly IDbHelper _dbHelper;
        private readonly IConfiguration _configuration;
        private readonly IIntegrationHelper _integrationHelper;

        public MultipleConsentQueryWorker(IConfiguration configuration, ILogger<MultipleConsentQueryWorker> logger, IDbHelper dbHelper, IIntegrationHelper integrationHelper)
        {
            _configuration = configuration;
            _logger = logger;
            _dbHelper = dbHelper;
            _integrationHelper = integrationHelper;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                bool errorFlag = false;
                int successCount = 0;
                int failedCount = 0;

                try
                {
                    _logger.LogInformation("MultipleConsentQueryWorker running at: {time}", DateTimeOffset.Now);
                    var batchCount = _configuration.GetValue<int>("MultipleConsentQueryBatchCount");
                    var batchList = await _dbHelper.GetUnprocessedMultipleConsenBatches(batchCount);
                    var queue = new ConcurrentQueue<BatchConsentQuery>(batchList);
                    var options = new ParallelOptions() { MaxDegreeOfParallelism = System.Environment.ProcessorCount };

                    // foreach (var batch in batchList)
                    Parallel.ForEach(queue, options, (q) =>
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
                        catch (Exception ex)
                        {
                            errorFlag = true;
                            failedCount++;

                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError("MultipleConsentQueryWorker Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");

                }

                if (errorFlag)
                    _logger.LogError($"MultipleConsentQueryWorker toplam {failedCount + successCount} içinden {failedCount} recepient için hata aldı, IYSConsentRequest tablosuna bakın");


                await Task.Delay(_configuration.GetValue<int>("MultipleConsentQueryDelay"), stoppingToken);
            }
        }
    }
}
