using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request.Consent;
using IYSIntegration.Application.Services.Models.Response.Consent;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;

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
        var responseUpdates = new ConcurrentBag<ResponseBase<AddConsentResult>>();
        int failedCount = 0;
        int successCount = 0;

        _logger.LogInformation("SingleConsentAddService started at {Time}", DateTimeOffset.Now);

        try
        {
            var logs = await _dbService.GetConsentRequests(false, rowCount);

            var groupedLogs = logs.GroupBy(l => _iysHelper.GetCompanyCode(l.IysCode));
            var consentCache = new ConcurrentDictionary<string, Dictionary<(string Recipient, string RecipientType), Consent>>();

            foreach (var group in groupedLogs)
            {
                var companyCode = group.Key;
                var recipientKeys = group
                    .Select(l => CreateRecipientKey(l.Recipient, l.RecipientType))
                    .Distinct()
                    .ToList();
                var existingRecipients = recipientKeys.Select(r => r.Recipient).Distinct();
                var existing = await _dbService.GetLastConsents(companyCode, existingRecipients);
                var dict = existing.ToDictionary(c => CreateRecipientKey(c.Recipient, c.RecipientType), c => c);
                var missing = recipientKeys.Where(r => !dict.ContainsKey(r)).ToList();

                if (missing.Any())
                {
                    var queryTasks = missing.Select(async recipientKey =>
                    {
                        var log = group.First(l => CreateRecipientKey(l.Recipient, l.RecipientType) == recipientKey);
                        try
                        {
                            var queryReq = new QueryConsentRequest
                            {
                                CompanyCode = companyCode,
                                IysCode = log.IysCode,
                                BrandCode = log.BrandCode,
                                RecipientKey = new RecipientKey
                                {
                                    Recipient = log.Recipient,
                                    RecipientType = log.RecipientType,
                                    Type = log.Type
                                }
                            };

                            var queryResp = await _client.PostJsonAsync<RecipientKey, QueryConsentResult>($"consents/{companyCode}/queryConsent", queryReq.RecipientKey);
                            if (queryResp.IsSuccessful() && queryResp.Data != null && !string.IsNullOrEmpty(queryResp.Data.ConsentDate))
                            {
                                var insertReq = new AddConsentRequest
                                {
                                    CompanyCode = companyCode,
                                    IysCode = log.IysCode,
                                    BrandCode = log.BrandCode,
                                    Consent = new Consent
                                    {
                                        Recipient = queryResp.Data.Recipient,
                                        Type = queryResp.Data.Type,
                                        Source = queryResp.Data.Source,
                                        Status = queryResp.Data.Status,
                                        ConsentDate = queryResp.Data.ConsentDate,
                                        RecipientType = queryResp.Data.RecipientType,
                                        CreationDate = queryResp.Data.CreationDate,
                                        TransactionId = queryResp.Data.TransactionId
                                    }
                                };
                                await _dbService.InsertPullConsent(insertReq);
                                dict[recipientKey] = insertReq.Consent;
                            }
                            else if (!queryResp.IsSuccessful())
                            {
                                results.Add(new LogResult
                                {
                                    Id = log.Id,
                                    CompanyCode = companyCode,
                                    Messages = new Dictionary<string, string>
                                    {
                                        { "Query Error", BuildErrorMessage(queryResp) }
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error querying consent for {Recipient}", recipientKey.Recipient);
                            results.Add(new LogResult
                            {
                                Id = log.Id,
                                CompanyCode = companyCode,
                                Messages = new Dictionary<string, string>
                                {
                                    { "Query Error", ex.Message }
                                }
                            });
                        }
                    });
                    await Task.WhenAll(queryTasks);
                }

                consentCache[companyCode] = dict;
            }

            var validLogs = new List<ConsentRequestLog>();
            foreach (var log in logs)
            {
                var companyCode = _iysHelper.GetCompanyCode(log.IysCode);
                consentCache.TryGetValue(companyCode, out var companyConsents);
                if (companyConsents == null)
                {
                    companyConsents = new Dictionary<(string Recipient, string RecipientType), Consent>();
                    consentCache[companyCode] = companyConsents;
                }
                companyConsents.TryGetValue(CreateRecipientKey(log.Recipient, log.RecipientType), out var existingConsent);

                if ((log.Status == "RED" || log.Status == "RET") && existingConsent == null)
                {
                    continue;
                }

                if (existingConsent != null &&
                    DateTime.TryParse(existingConsent.ConsentDate, out var lastDate) &&
                    DateTime.TryParse(log.ConsentDate, out var reqDate) &&
                    lastDate > reqDate)
                {
                    continue;
                }

                if (existingConsent != null && existingConsent.Status == "RET" && log.Status == "RET")
                {
                    continue;
                }

                if (existingConsent != null && !string.IsNullOrWhiteSpace(existingConsent.ConsentDate) &&
                        DateTime.TryParse(existingConsent.ConsentDate, out var consentDate) &&
                        _iysHelper.IsOlderThanBusinessDays(consentDate, 3))
                {
                    continue;
                }

                validLogs.Add(log);
            }

            foreach (var log in validLogs)
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

                    var addResponse = await _client.PostJsonAsync<Consent, AddConsentResult>($"consents/{companyCode}/addConsent", request.Consent);

                    addResponse.Id = log.Id;
                    responseUpdates.Add(addResponse);

                    if (addResponse.IsSuccessful() && addResponse.HttpStatusCode >= 200 && addResponse.HttpStatusCode < 300)
                    {
                        Interlocked.Increment(ref successCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref failedCount);
                    }

                }
                catch (Exception ex)
                {
                    results.Add(new LogResult { Id = log.Id, CompanyCode = companyCode, Messages = new Dictionary<string, string> { { "Exception", ex.Message } } });
                    Interlocked.Increment(ref failedCount);
                    _logger.LogError(ex, "Exception in SingleConsentAddService for log ID {Id}", log.Id);
                    response.Error();
                }
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in SingleConsentAddService");
            results.Add(new LogResult { Id = 0, CompanyCode = "", Status = "Failed", Messages = new Dictionary<string, string> { { "Exception", ex.Message } } });
            response.Error("SINGLE_CONSENT_ADD_FATAL", "Service failed with an unexpected exception.");
        }

        try
        {
            var responsesToUpdate = responseUpdates.ToArray();
            if (responsesToUpdate.Length > 0)
            {
                await _dbService.UpdateConsentResponses(responsesToUpdate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk update of consent responses failed");
            results.Add(new LogResult
            {
                Id = 0,
                CompanyCode = string.Empty,
                Messages = new Dictionary<string, string>
                {
                    { "Database Error", ex.Message }
                }
            });
            response.Error("CONSENT_RESPONSE_UPDATE_FAILED", "Consent responses could not be updated.");
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

    private static (string Recipient, string RecipientType) CreateRecipientKey(string recipient, string? recipientType)
        => (recipient, recipientType ?? string.Empty);

    private static string BuildErrorMessage<T>(ResponseBase<T> response)
    {
        var parts = new List<string>();

        if (response.Messages is { Count: > 0 })
        {
            parts.AddRange(response.Messages.Select(kv => $"{kv.Key}: {kv.Value}"));
        }

        if (!string.IsNullOrWhiteSpace(response.OriginalError?.Message))
        {
            parts.Add($"Message: {response.OriginalError.Message}");
        }

        if (response.OriginalError?.Errors != null && response.OriginalError.Errors.Length > 0)
        {
            parts.AddRange(response.OriginalError.Errors
                .Where(e => !string.IsNullOrWhiteSpace(e.Code) || !string.IsNullOrWhiteSpace(e.Message))
                .Select(e => string.IsNullOrWhiteSpace(e.Code)
                    ? e.Message ?? string.Empty
                    : string.IsNullOrWhiteSpace(e.Message)
                        ? e.Code
                        : $"{e.Code}: {e.Message}"));
        }

        if (parts.Count == 0)
        {
            parts.Add($"HTTP {response.HttpStatusCode}");
        }

        return string.Join(" | ", parts);
    }
}
