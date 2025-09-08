using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Response.Consent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace IYSIntegration.Application.Services;

public class ScheduledMultipleConsentAddService
{
    private readonly ILogger<ScheduledMultipleConsentAddService> _logger;
    private readonly IDbService _dbService;
    private readonly IConfiguration _configuration;
    private readonly IysProxy _client;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public ScheduledMultipleConsentAddService(IConfiguration configuration, ILogger<ScheduledMultipleConsentAddService> logger, IDbService dbHelper, IysProxy client)
    {
        _configuration = configuration;
        _logger = logger;
        _dbService = dbHelper;
        _client = client;
    }

    public async Task<ResponseBase<ScheduledJobStatistics>> RunAsync(int batchSize, int batchCount, int checkAfterInSeconds)
    {
        var response = new ResponseBase<ScheduledJobStatistics>();
        var successCount = 0;
        var failedCount = 0;
        var results = new ConcurrentBag<LogResult>();

        _logger.LogInformation("MultipleConsentAddService running at: {time}", DateTimeOffset.Now);

        try
        {
            var companyList = _configuration.GetSection("CompanyCodes").Get<List<string>>() ?? new();

            foreach (var companyCode in companyList)
            {
                await _dbService.UpdateBatchId(companyCode, batchSize);
                var batchList = await _dbService.GetBatchSummary(batchCount);

                foreach (var batch in batchList)
                {
                    try
                    {
                        var consentRequests = await _dbService.GeBatchConsentRequests(batch.BatchId);
                        var first = consentRequests.FirstOrDefault();
                        if (first == null) continue;

                        var consents = consentRequests.Select(x => new Consent
                        {
                            ConsentDate = x.ConsentDate,
                            Recipient = x.Recipient,
                            RecipientType = x.RecipientType,
                            Source = x.Source,
                            Status = x.Status,
                            Type = x.Type
                        }).ToList();

                        var result = await _client.PostJsonAsync<List<Consent>, MultipleConsentResult>($"{companyCode}/multipleConsent", consents);

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

                            await _semaphore.WaitAsync();
                            try
                            {
                                await _dbService.UpdateBatchConsentRequests(batchConsentQuery);
                            }
                            finally
                            {
                                _semaphore.Release();
                            }

                            Interlocked.Increment(ref successCount);
                        }
                        else if (result.HttpStatusCode == 422 && result.OriginalError?.Errors?.Length > 0)
                        {
                            await _semaphore.WaitAsync();
                            try
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

                                    await _dbService.UpdateMultipleConsentItem(batchItemResult);
                                }

                                await _dbService.ReorderBatch(batch.BatchId);
                            }
                            finally
                            {
                                _semaphore.Release();
                            }

                            Interlocked.Increment(ref failedCount);
                            _logger.LogError($"MultipleConsentAddService {batch.BatchId} hata aldı. IYSConsentRequest tablosuna göz atın");
                            results.Add(new LogResult { Id = batch.BatchId, CompanyCode = companyCode, Status = "Failed", Messages = response.Messages});
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"MultipleConsentAddService {companyCode} hata aldı. IYSConsentRequest tablosuna göz atın");
                        results.Add(new LogResult { Id = batch.BatchId, CompanyCode = companyCode, Status = "Failed", Messages = response.Messages });
                        Interlocked.Increment(ref failedCount);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MultipleConsentAddService genel hata");
            results.Add(new LogResult { Id = 0, CompanyCode = "", Status = "Failed", Messages = new Dictionary<string, string> { { "Exception", ex.Message } } });
        }

        foreach (var result in results)
        {
            response.AddMessage(result.GetMessages());
        }

        response.Data = new ScheduledJobStatistics { SuccessCount = successCount, FailedCount = failedCount };

        return response;
    }
}
