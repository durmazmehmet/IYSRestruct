using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Common.Worker.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IYSIntegration.Common.Worker.Services
{
    public class MultipleConsentService
    {
        private readonly ILogger<MultipleConsentService> _logger;
        private readonly IWorkerDbHelper _dbHelper;
        private readonly IConfiguration _configuration;
        private readonly IIntegrationHelper _integrationHelper;
        private readonly object obj = new object();

        public MultipleConsentService(IConfiguration configuration, ILogger<MultipleConsentService> logger, IWorkerDbHelper dbHelper, IIntegrationHelper integrationHelper)
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
            int successCount = 0;
            int failedCount = 0;
            try
            {
                _logger.LogInformation("MultipleConsentService running at: {time}", DateTimeOffset.Now);

                var checkAfterInSeconds = 60; // previously from MultipleConsentQueryCheckAfter
                var batchSize = _configuration.GetValue<int>("MultipleConsentBatchSize");

                var companyList = new List<string> { "BOI", "BOP", "BOPK", "BOM" };
                foreach (var companyCode in companyList)
                {
                    await _dbHelper.UpdateBatchId(companyCode, batchSize);
                }

                var batchCount = _configuration.GetValue<int>("MultipleConsentBatchCount");
                var batchList = await _dbHelper.GetBatchSummary(batchCount);

                foreach (var batch in batchList)
                {
                    try
                    {
                        var consentRequests = await _dbHelper.GeBatchConsentRequests(batch.BatchId);
                        var request = new MultipleConsentRequest
                        {
                            IysCode = consentRequests[0].IysCode,
                            BrandCode = consentRequests[0].BrandCode,
                            Consents = new List<Consent>(),
                            BatchId = batch.BatchId,
                            ForBatch = true
                        };

                        foreach (var consent in consentRequests)
                        {
                            request.Consents.Add(new Consent
                            {
                                ConsentDate = consent.ConsentDate,
                                Recipient = consent.Recipient,
                                RecipientType = consent.RecipientType,
                                Source = consent.Source,
                                Status = consent.Status,
                                Type = consent.Type
                            });
                        }

                        var result = await _integrationHelper.SendMultipleConsent(request);
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
                            if (result.HttpStatusCode == 422 && result.OriginalError?.Errors?.Length > 0)
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
                            failedCount++;
                        }
                    }
                    catch (Exception)
                    {
                        errorFlag = true;
                        failedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                errorFlag = true;
                _logger.LogError("MultipleConsentService hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
            }

            response.Data = new ProcessResult { SuccessCount = successCount, FailedCount = failedCount };

            if (errorFlag || failedCount > 0)
                response.Error("FailedCount", failedCount.ToString());

            return response;
        }
    }
}
