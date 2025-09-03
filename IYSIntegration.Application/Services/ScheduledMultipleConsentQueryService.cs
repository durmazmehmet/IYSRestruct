using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Base;
using IYSIntegration.Application.Request.Consent;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
                var options = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };

                await Parallel.ForEachAsync(batchList, options, async (batch, _) =>
                {
                    try
                    {
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

                                    await _dbService.UpdateMultipleConsentItem(batchItemResult);
                                }

                                await _dbService.UpdateMultipleConsentQueryDate(batch.BatchId, result.LogId);
                            }
                            Interlocked.Increment(ref successCount);
                        }
                    }
                    catch
                    {
                        errorFlag = true;
                        Interlocked.Increment(ref failedCount);
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
