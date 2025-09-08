using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request.Consent;
using IYSIntegration.Application.Services.Models.Response.Consent;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace IYSIntegration.Application.Services;


public class ScheduledSingleConsentAddService
{
    private readonly ILogger<ScheduledSingleConsentAddService> _logger;
    private readonly IDbService _dbService;
    private readonly IysProxy _client;
    private readonly IIysHelper _iysHelper;

    public ScheduledSingleConsentAddService(ILogger<ScheduledSingleConsentAddService> logger, IDbService dbHelper, IIysHelper iysHelper, IysProxy client)
    {
        _logger = logger;
        _dbService = dbHelper;
        _client = client;
        _iysHelper = iysHelper;
    }

    public async Task<ResponseBase<ScheduledJobStatistics>> RunAsync(int rowCount)
    {
        var response = new ResponseBase<ScheduledJobStatistics>();
        response.Success();
        var results = new ConcurrentBag<LogResult>();
        int failedCount = 0;
        int successCount = 0;

        _logger.LogInformation("SingleConsentAddService started at {Time}", DateTimeOffset.Now);

        try
        {
            var logs = await _dbService.GetConsentRequests(false, rowCount);

            var tasks = logs.Select(async log =>
            {
                var companyCode = _iysHelper.GetCompanyCode(log.IysCode);

                try
                {
                    var request = new AddConsentRequest
                    {
                        WithoutLogging = true,
                        IysCode = log.IysCode,
                        BrandCode = log.BrandCode,
                        Consent = new Consent
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
                        _iysHelper.IsOlderThanBusinessDays(consentDate, 3))
                    {
                        results.Add(new LogResult { Id = log.Id, CompanyCode = request.CompanyCode, Messages = new Dictionary<string, string> { { "Skipped", "Consent older than 3 business days" } } });
                        Interlocked.Increment(ref failedCount);
                        _logger.LogWarning("Consent ID {Id} skipped: older than 3 business days", log.Id);
                        return;
                    }

                    var addResponse = await _client.PostJsonAsync<Consent, AddConsentResult> ($"consents/{companyCode}/addConsent", request.Consent);

                    if (!request.WithoutLogging)
                    {
                        var id = await _dbService.InsertConsentRequest(request);
                        addResponse.Id = id;
                        await _dbService.UpdateConsentResponseFromCommon(addResponse);
                        addResponse.OriginalError = null;
                    }

                    if (addResponse.HttpStatusCode == 0 || addResponse.HttpStatusCode >= 500)
                    {
                        results.Add(new LogResult { Id = log.Id, Status = "Failed", Messages = response.Messages });
                        Interlocked.Increment(ref failedCount);
                        _logger.LogError("AddConsent failed (status: {Status}) for log ID {Id}", addResponse.HttpStatusCode, log.Id);
                        return;
                    }

                    addResponse.Id = log.Id;
                    await _dbService.UpdateConsentResponse(addResponse);
                    Interlocked.Increment(ref successCount);

                   results.Add(new LogResult { Id = log.Id, Messages = new Dictionary<string, string> { {log.Id.ToString(), addResponse.Data.TransactionId.ToString() } } });
                }
                catch (Exception ex)
                {
                    results.Add(new LogResult { Id = log.Id, CompanyCode = companyCode, Messages = new Dictionary<string, string> { { "Exception", ex.Message } } });
                    Interlocked.Increment(ref failedCount);
                    _logger.LogError(ex, "Exception in SingleConsentAddService for log ID {Id}", log.Id);
                    response.Error();
                }
            });

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in SingleConsentAddService");
            results.Add(new LogResult { Id = 0, CompanyCode = "", Status = "Failed", Messages = new Dictionary<string, string> { { "Exception", ex.Message } } });
            response.Error("SINGLE_CONSENT_ADD_FATAL", "Service failed with an unexpected exception.");
        }

        foreach (var result in results)
        {
            response.AddMessage(result.GetMessages());
        }
        response.Data = new ScheduledJobStatistics
        {
            SuccessCount = successCount,
            FailedCount = failedCount
        };
        return response;
    }

}
