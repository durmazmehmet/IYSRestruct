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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IYSIntegration.WorkerService.Services
{
    public class MultipleConsentAddWorker : BackgroundService
    {
        private readonly ILogger<MultipleConsentAddWorker> _logger;
        private readonly IDbHelper _dbHelper;
        private readonly IConfiguration _configuration;
        private readonly IIntegrationHelper _integrationHelper;
        private object obj = new Object();

        public MultipleConsentAddWorker(IConfiguration configuration, ILogger<MultipleConsentAddWorker> logger, IDbHelper dbHelper, IIntegrationHelper integrationHelper)
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
                    _logger.LogInformation("MultipleConsentAddWorker running at: {time}", DateTimeOffset.Now);

                    var checkAfterInSeconds = _configuration.GetValue<int>("MultipleConsentQueryCheckAfter");
                    var batchSize = _configuration.GetValue<int>("MultipleConsentBatchSize");

                    var companyList = new List<string> { "BOI", "BOP", "BOPK", "BOM" };
                    foreach (var companyCode in companyList)
                    {
                        await _dbHelper.UpdateBatchId(companyCode, batchSize);
                    }

                    var batchCount = _configuration.GetValue<int>("MultipleConsentBatchCount");
                    var batchList = await _dbHelper.GetBatchSummary(batchCount);

                    var queue = new ConcurrentQueue<BatchSummary>(batchList);
                    var options = new ParallelOptions() { MaxDegreeOfParallelism = System.Environment.ProcessorCount };
                    foreach (var batch in batchList)
                    //Parallel.ForEach(queue, options, (q) =>
                    {
                        try
                        {
                            //var batch = new BatchSummary();
                            //if (!queue.TryDequeue(out batch))
                            //{
                            //    return;
                            //}

                            var consentRequests = _dbHelper.GeBatchConsentRequests(batch.BatchId).Result;
                            var request = new MultipleConsentRequest
                            {
                                IysCode = consentRequests[0].IysCode,
                                BrandCode = consentRequests[0].BrandCode,
                                Consents = new List<Common.Base.Consent>(),
                                BatchId = batch.BatchId,
                                ForBatch = true
                            };

                            foreach (var consent in consentRequests)
                            {
                                request.Consents.Add(new Common.Base.Consent
                                {
                                    ConsentDate = consent.ConsentDate,
                                    Recipient = consent.Recipient,
                                    RecipientType = consent.RecipientType,
                                    Source = consent.Source,
                                    Status = consent.Status,
                                    Type = consent.Type
                                });
                            }

                            var result = _integrationHelper.SendMultipleConsent(request).Result;
                            if (result.IsSuccessful())
                            {
                                var batchConsentQuery = new BatchConsentQuery
                                {
                                    IysCode = batch.IysCode,
                                    BrandCode = batch.BrandCode,
                                    BatchId = batch.BatchId,
                                    LogId = result.LogId,
                                    RequestId = result.Data.RequestId.ToString(),
                                    CheckAfter = checkAfterInSeconds
                                };

                                lock (obj)
                                {
                                    _dbHelper.UpdateBatchConsentRequests(batchConsentQuery).Wait();
                                }
                                successCount++;
                            }
                            else
                            {
                                if (result.HttpStatusCode == 422)
                                {
                                    if (result.OriginalError?.Errors?.Length > 0)
                                    {
                                        lock (obj)
                                        {
                                            foreach (var error in result.OriginalError.Errors)
                                            {
                                                var batchItemResult = new BatchItemResult
                                                {
                                                    BatchId = batch.BatchId,
                                                    Index = error.Index + 1,
                                                    IsSuccess = false,
                                                    BatchError = JsonConvert.SerializeObject(error, Formatting.None, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }),
                                                    LogId = result.LogId,
                                                    IsQueryResult = false
                                                };

                                                _dbHelper.UpdateMultipleConsentItem(batchItemResult).Wait();
                                            }

                                            _dbHelper.ReorderBatch(batch.BatchId).Wait();
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            errorFlag = true;
                            failedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("MultipleConsentAddWorker hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
                }

                if (errorFlag)
                    _logger.LogError($"MultipleConsentAddWorker toplam {failedCount + successCount} içinden {failedCount} recepient için hata aldı. , IYSConsentRequest tablosuna göz atın");

                await Task.Delay(_configuration.GetValue<int>("MultipleConsentRequestDelay"), stoppingToken);
            }
        }
    }
}
