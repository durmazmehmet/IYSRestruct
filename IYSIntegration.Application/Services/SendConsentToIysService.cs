using System;
using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request;
using IYSIntegration.Application.Services.Models.Response.Consent;
using IYSIntegration.Application.Services.Models.Response.Schedule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;


namespace IYSIntegration.Application.Services;

public class SendConsentToIysService
{
    private readonly ILogger<SendConsentToIysService> _logger;
    private readonly IDbService _dbService;
    private readonly IIysProxy _client;
    private readonly IIysHelper _iysHelper;
    private static readonly HashSet<string> ApprovalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "ON",
        "ONAY"
    };
    private static readonly HashSet<string> RejectionStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "RET",
        "RED"
    };
    private const int ConsentFreshnessDays = 3;
    private static readonly HashSet<string> OverdueErrorCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "H174",
        "H175",
        "H178"
    };
    private sealed record QueryMultipleConsentCache(bool Success, HashSet<string> Recipients);

    public SendConsentToIysService(
        ILogger<SendConsentToIysService> logger,
        IDbService dbHelper,
        IIysHelper iysHelper,
        IIysProxy client,
        IConfiguration configuration)
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
        var responseUpdates = new ConcurrentBag<ConsentResponseUpdate>();
        int failedCount = 0;
        int successCount = 0;

        try
        {
            // Bekleyen izin kayıtlarını çekip şirket bazında gruplayacağız.
            var pendingConsents = await _dbService.GetPendingConsents(rowCount);
            var groupedConsents = new Dictionary<string, List<ConsentRequestLog>>(StringComparer.OrdinalIgnoreCase);

            foreach (var log in pendingConsents)
            {
                var companyCode = !string.IsNullOrWhiteSpace(log.CompanyCode)
                    ? log.CompanyCode
                    : _iysHelper.GetCompanyCode(log.IysCode) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(companyCode))
                {
                    results.Add(new LogResult
                    {
                        Id = log.Id,
                        CompanyCode = string.Empty,
                        Messages = new Dictionary<string, string>
                        {
                            { "Company", "Şirket kodu bulunamadı." }
                        }
                    });
                    Interlocked.Increment(ref failedCount);
                    continue;
                }

                log.CompanyCode = companyCode;

                if (!groupedConsents.TryGetValue(companyCode, out var consentList))
                {
                    consentList = new List<ConsentRequestLog>();
                    groupedConsents.Add(companyCode, consentList);
                }

                consentList.Add(log);
            }

            foreach (var group in groupedConsents)
            {

                var queryCache = new Dictionary<string, QueryMultipleConsentCache>();

                foreach (var log in group.Value)
                {
                    var cacheKey = BuildRecipientTypeCacheKey(log);

                    if (!queryCache.TryGetValue(cacheKey, out var queryInfo))
                    {
                        var recipients = group.Value
                            .Where(x => string.Equals(x.RecipientType, log.RecipientType, StringComparison.OrdinalIgnoreCase)
                                && string.Equals(x.Type, log.Type, StringComparison.OrdinalIgnoreCase))
                            .Select(x => NormalizeRecipient(x.Recipient))
                            .Where(r => !string.IsNullOrWhiteSpace(r))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        queryInfo = await QueryExistingConsentsAsync(group.Key, log.RecipientType, log.Type, recipients);
                        queryCache[cacheKey] = queryInfo;
                    }

                    var skipReason = GetSkipReason(log, queryInfo);

                    if (!string.IsNullOrEmpty(skipReason))
                    {
                        results.Add(new LogResult
                        {
                            Id = log.Id,
                            CompanyCode = group.Key,
                            Status = "Skipped",
                            Messages = new Dictionary<string, string>
                            {
                                { "Skip", skipReason }
                            }
                        });

                        responseUpdates.Add(new ConsentResponseUpdate
                        {
                            Id = log.Id,
                            LogId = log.LogId ?? 0,
                            IsSuccess = false,
                            TransactionId = null,
                            CreationDate = null,
                            BatchError = skipReason,
                            IsOverdue = true
                        });

                        Interlocked.Increment(ref failedCount);
                        continue;
                    }

                    try
                    {
                        var pendingRecipient = pending.Recipient?.Trim();
                        if (string.IsNullOrWhiteSpace(pendingRecipient))
                        {
                            continue;
                        }

                        if (!latestPendingByRecipient.ContainsKey(pendingRecipient))
                        {
                            latestPendingByRecipient[pendingRecipient] = pending;
                        }
                    }

                    // ON durumlu kayıtlar için IYS'deki son onayları sorguluyoruz.
                    if (consentGroup.Any(log => string.Equals(log.Status?.Trim(), "ON", StringComparison.OrdinalIgnoreCase)))
                    {
                        var queryInfo = await GetApprovedConsentEntriesAsync(
                            companyCode,
                            recipientType,
                            string.IsNullOrWhiteSpace(consentType) ? null : consentType,
                            recipients);

                        if (queryInfo.Messages.Count > 0)
                        {
                            response.AddMessage(queryInfo.Messages);
                        }

                        foreach (var entry in queryInfo.Entries)
                        {

                            Interlocked.Increment(ref successCount);
                            var normalizedRecipient = NormalizeRecipient(log.Recipient);
                            if (!string.IsNullOrWhiteSpace(normalizedRecipient))
                            {
                                queryInfo.Recipients.Add(normalizedRecipient);
                            }
                        }
                    }

                    foreach (var log in consentGroup)
                    {
                        // Gönderimden önce iş kurallarını uyguluyoruz.
                        ConsentRequestLog? latestPending = null;
                        var trimmedRecipient = log.Recipient?.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmedRecipient))
                        {
                            latestPendingByRecipient.TryGetValue(trimmedRecipient, out latestPending);
                        }

                        if (ShouldSkipConsent(log, approvedRecipients, latestPending, out var skipReason))
                        {
                            responseUpdates.Add(CreateOverdueUpdate(log, skipReason));
                            results.Add(new LogResult
                            {
                                Id = log.Id,
                                CompanyCode = companyCode,
                                Status = "Skipped",
                                Messages = new Dictionary<string, string>
                                {
                                    { "SkipReason", skipReason }
                                }
                            });
                            continue;
                        }

                        try
                        {
                            // IYS servis çağrısında kullanılacak isteği hazırlıyoruz.
                            var request = new AddConsentRequest
                            {
                                IysCode = log.IysCode,
                                BrandCode = log.BrandCode,
                                CompanyCode = companyCode,
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

                            var addResponse = await _client.PostJsonAsync<Consent, AddConsentResult>("consents/{companyCode}/addConsent", request.Consent);

                            addResponse.Id = log.Id;

                            var update = CreateConsentResponseUpdate(addResponse, out var addMessage);
                            if (update != null)
                            {
                                responseUpdates.Add(update);
                            }

                            if (addMessage.HasValue)
                            {
                                response.AddMessage(addMessage.Value.Key, addMessage.Value.Value);
                            }

                            if (addResponse.IsSuccessful() && addResponse.HttpStatusCode >= 200 && addResponse.HttpStatusCode < 300)
                            {
                                Interlocked.Increment(ref successCount);
                            }
                            else
                            {
                                results.Add(new LogResult
                                {
                                    Id = log.Id,
                                    CompanyCode = companyCode,
                                    Messages = new Dictionary<string, string>
                                    {
                                        { "Add Error", BuildErrorMessage(addResponse) }
                                    }
                                });
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

                    queryCache[cacheKey] = queryInfo;
                }
            }


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in SingleConsentAddService");
            results.Add(new LogResult { Id = 0, CompanyCode = "", Status = "Failed", Messages = new Dictionary<string, string> { { "Exception", ex.Message } } });
            response.Error("SINGLE_CONSENT_ADD_FATAL", "Service failed with an unexpected exception.");
        }

        var updatesToPersist = responseUpdates.ToList();

        if (updatesToPersist.Any())
        {
            foreach (var update in updatesToPersist)
            {
                ApplyConsentResponseBusinessRules(update);
            }
            try
            {
                await _dbService.UpdateConsentResponses(updatesToPersist);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Failed to update consent responses after processing consents.");
            }
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

        // Tüm toplu mesajları tek seferde logluyoruz.
        if (response.Messages is { Count: > 0 })
        {
            foreach (var message in response.Messages)
            {
                _logger.LogInformation("SingleConsentWorker mesaj {Key}: {Value}", message.Key, message.Value);
            }
        }

        return response;
    }


    private static string BuildRecipientTypeCacheKey(ConsentRequestLog log)
    {
        var recipientType = log.RecipientType?.Trim().ToUpperInvariant() ?? string.Empty;
        var type = log.Type?.Trim().ToUpperInvariant() ?? string.Empty;

        return $"{recipientType}|{type}";
    }

    private async Task<QueryMultipleConsentCache> QueryExistingConsentsAsync(
        string companyCode,
        string? recipientType,
        string? consentType,
        IReadOnlyCollection<string> recipients)
    {
        if (string.IsNullOrWhiteSpace(companyCode)
            || string.IsNullOrWhiteSpace(recipientType)
            || string.IsNullOrWhiteSpace(consentType)
            || recipients == null
            || recipients.Count == 0)
        {
            return CreateEmptyQueryResult();
        }

        try
        {
            var request = new RecipientKeyWithList
            {
                RecipientType = recipientType,
                Type = consentType,
                Recipients = recipients
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Select(r => r.Trim())
                    .ToList()
            };

            var response = await _client.PostJsonAsync<RecipientKeyWithList, MultipleQueryConsentResult>(
                $"consents/{companyCode}/queryMultipleConsent",
                request);

            if (response.IsSuccessful() && response.HttpStatusCode >= 200 && response.HttpStatusCode < 300)
            {
                var recipientsFromResponse = (response.Data?.List ?? new List<string>())
                    .Select(NormalizeRecipient)
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                return new QueryMultipleConsentCache(true, recipientsFromResponse);
            }

            _logger.LogWarning(
                "queryMultipleConsent failed for company {CompanyCode} ({RecipientType}/{Type}) with status {Status}",
                companyCode,
                recipientType,
                consentType,
                response.HttpStatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception while querying existing consents for company {CompanyCode} ({RecipientType}/{Type})",
                companyCode,
                recipientType,
                consentType);
        }

        return CreateEmptyQueryResult();
    }

    private static QueryMultipleConsentCache CreateEmptyQueryResult()
        => new(false, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    private static string? NormalizeRecipient(string? recipient)
        => string.IsNullOrWhiteSpace(recipient) ? null : recipient.Trim();

    private static string? GetSkipReason(ConsentRequestLog log, QueryMultipleConsentCache queryInfo)
    {
        if (IsConsentOlderThanThreshold(log.ConsentDate, out var dateReason))
        {
            return dateReason;
        }

        if (!queryInfo.Success)
        {
            return null;
        }

        var normalizedRecipient = NormalizeRecipient(log.Recipient);
        var existsInList = !string.IsNullOrWhiteSpace(normalizedRecipient)
            && queryInfo.Recipients.Contains(normalizedRecipient!);

        var status = log.Status?.Trim();

        if (IsApprovalStatus(status) && existsInList)
        {
            return "Rıza zaten IYS listesinde mevcut.";
        }

        if (IsRejectionStatus(status) && !existsInList)
        {
            return "IYS listesinde olmayan rıza için ret gönderilemez.";
        }

        return null;
    }

    private static bool IsConsentOlderThanThreshold(string? consentDate, out string? reason)
    {
        reason = null;

        if (string.IsNullOrWhiteSpace(consentDate))
        {
            return false;
        }

        if (DateTimeOffset.TryParse(
                consentDate,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedDate))
        {
            if (parsedDate < DateTimeOffset.UtcNow.AddDays(-ConsentFreshnessDays))
            {
                reason = "Rıza tarihi 3 günden eski.";
                return true;
            }
        }

        return false;
    }

    private static bool IsApprovalStatus(string? status)
        => !string.IsNullOrWhiteSpace(status) && ApprovalStatuses.Contains(status.Trim());

    private static bool IsRejectionStatus(string? status)
        => !string.IsNullOrWhiteSpace(status) && RejectionStatuses.Contains(status.Trim());

    private ConsentResponseUpdate? CreateConsentResponseUpdate(ResponseBase<AddConsentResult> response)
    {
        responseMessage = null;

        if (response == null)
        {
            return null;
        }

        var errorCodeList = response.OriginalError?.Errors?
            .Select(x => x.Code)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToList() ?? new List<string>();

        var isConsentOverdue = response.OriginalError?.Errors?.Any(x =>
            !string.IsNullOrWhiteSpace(x.Code) && OverdueErrorCodes.Contains(x.Code!)) ?? false;

        var errors = errorCodeList.Count > 0 ? string.Join(",", errorCodeList) : "Mevcut olmayan";

        if (response.HttpStatusCode == 200)
        {
            responseMessage = new KeyValuePair<string, string>(
                $"add.success.{response.Id}",
                $"Log {response.Id} için IYS gönderimi başarıyla tamamlandı (HTTP {response.HttpStatusCode}).");
        }
        else if (isConsentOverdue)
        {
            responseMessage = new KeyValuePair<string, string>(
                $"add.overdue.{response.Id}",
                $"Log {response.Id} için IYS yanıtı gecikmeli veya mükerrer: {errors}.");
        }
        else
        {
            responseMessage = new KeyValuePair<string, string>(
                $"add.error.{response.Id}",
                $"Log {response.Id} için IYS gönderimi başarısız: {BuildErrorMessage(response)}");
        }

        var serializedError = response.OriginalError == null
            ? null
            : JsonConvert.SerializeObject(
                response.OriginalError,
                Formatting.None,
                new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });

        return new ConsentResponseUpdate
        {
            Id = response.Id,
            LogId = response.LogId,
            IsSuccess = isConsentOverdue ? false : response.IsSuccessful(),
            TransactionId = response.Data?.TransactionId,
            CreationDate = response.Data?.CreationDate,
            BatchError = serializedError,
            IsOverdue = isConsentOverdue
        };
    }


    private static void ApplyConsentResponseBusinessRules(ConsentResponseUpdate update)
    {
        if (update == null)
        {
            return;
        }

        if (update.IsOverdue)
        {
            update.IsSuccess = false;
        }

        if (update.IsSuccess)
        {
            update.IsOverdue = false;
            update.BatchError = null;
        }
        else if (string.IsNullOrWhiteSpace(update.BatchError))
        {
            update.BatchError = "Unknown error";
        }
    }

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
