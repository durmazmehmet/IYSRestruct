using IYSIntegration.Application.Interface;
using IYSIntegration.Application.Models;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace IYSIntegration.Application.Services
{
    public class MultipleConsentAddService
    {
        private readonly ILogger<MultipleConsentAddService> _logger;
        private readonly IDbService _dbService;
        private readonly IConfiguration _configuration;
        private readonly IIntegrationService _integrationService;
        private readonly object obj = new object();

        public MultipleConsentAddService(IConfiguration configuration, ILogger<MultipleConsentAddService> logger, IDbService dbHelper, IIntegrationService integrationHelper)
        {
            _configuration = configuration;
            _logger = logger;
            _dbService = dbHelper;
            _integrationService = integrationHelper;
        }

        public async Task RunAsync(int batchSize, int batchCount, int checkAfterInSeconds)
        {
            bool errorFlag = false;
            int successCount = 0;
            int failedCount = 0;
            try
            {
                _logger.LogInformation("MultipleConsentAddService running at: {time}", DateTimeOffset.Now);

                var companyList = _configuration.GetSection("CompanyCodes").Get<List<string>>() ?? new List<string>();
                foreach (var companyCode in companyList)
                {
                    await _dbService.UpdateBatchId(companyCode, batchSize);
                }

                var batchList = await _dbService.GetBatchSummary(batchCount);

                foreach (var batch in batchList)
                {
                    try
                    {
                        var consentRequests = _dbService.GeBatchConsentRequests(batch.BatchId).Result;
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

                        var result = _integrationService.SendMultipleConsent(request).Result;
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
                                _dbService.UpdateBatchConsentRequests(batchConsentQuery).Wait();
                            }
                            successCount++;
                        }
                        else if (result.HttpStatusCode == 422 && result.OriginalError?.Errors?.Length > 0)
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

                                    _dbService.UpdateMultipleConsentItem(batchItemResult).Wait();
                                }

                                _dbService.ReorderBatch(batch.BatchId).Wait();
                            }
                        }
                    }
                    catch
                    {
                        errorFlag = true;
                        failedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("MultipleConsentAddService hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
            }

            if (errorFlag)
                _logger.LogError($"MultipleConsentAddService toplam {failedCount + successCount} içinden {failedCount} recipient için hata aldı. , IYSConsentRequest tablosuna göz atın");
        }
    }
}
