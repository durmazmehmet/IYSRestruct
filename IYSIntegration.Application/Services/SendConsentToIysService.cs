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
    private static readonly HashSet<string> OverdueErrorCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "H174",
        "H175",
        "H178"
    };

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
                var companyCode = group.Key;

                var groupedByRecipientAndType = group.Value
                    .GroupBy(log => new ConsentGroupKey(
                            log.RecipientType?.Trim() ?? string.Empty,
                            log.Type?.Trim() ?? string.Empty),
                        ConsentGroupKeyComparer.Instance);

                foreach (var consentGroup in groupedByRecipientAndType)
                {
                    // Aynı kanal ve iletişim türü için bekleyen kayıtları birlikte değerlendiriyoruz.
                    var recipientType = consentGroup.Key.RecipientType;
                    var consentType = consentGroup.Key.Type;

                    var recipients = consentGroup
                        .Select(log => log.Recipient)
                        .Where(recipient => !string.IsNullOrWhiteSpace(recipient))
                        .Select(recipient => recipient!.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();


                    var approvedRecipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    // Bekleyen kayıtlar arasındaki en güncel log'u belirlemek için bir sözlük tutuyoruz.
                    var latestPendingByRecipient = new Dictionary<string, ConsentRequestLog>(StringComparer.OrdinalIgnoreCase);
                    foreach (var pending in consentGroup)
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
                            var trimmedRecipient = entry?.Recipient?.Trim();
                            if (string.IsNullOrWhiteSpace(trimmedRecipient))
                            {
                                continue;
                            }

                            approvedRecipients.Add(trimmedRecipient);
                            if (!string.IsNullOrWhiteSpace(entry.Reason))
                            {
                                response.AddMessage(
                                    $"query.reason.{companyCode}.{trimmedRecipient}",
                                    entry.Reason!.Trim());
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
        if (updatesToPersist.Count > 0)
        {
            try
            {
                await _dbService.UpdateConsentResponses(updatesToPersist);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist consent response updates.");
                response.AddMessage("PersistError", ex.Message);
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

    private ConsentResponseUpdate? CreateConsentResponseUpdate(
        ResponseBase<AddConsentResult> response,
        out KeyValuePair<string, string>? responseMessage)
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

    // Gönderim öncesi iş kurallarını tek noktada değerlendiriyoruz.
    private bool ShouldSkipConsent(
        ConsentRequestLog log,
        ISet<string> approvedRecipients,
        ConsentRequestLog? latestPending,
        out string reason)
    {
        reason = string.Empty;

        if (log == null)
        {
            return false;
        }

        var recipient = log.Recipient?.Trim();
        var status = log.Status?.Trim();

        if (string.IsNullOrWhiteSpace(recipient) || string.IsNullOrWhiteSpace(status))
        {
            return false;
        }


        approvedRecipients ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (latestPending != null && latestPending.Id != log.Id)
        {
            reason =
                $"SKIP_STALE_PENDING: Recipient {recipient} has a newer pending consent entry with log ID {latestPending.Id}.";
            return true;
        }

        if (string.Equals(status, "ON", StringComparison.OrdinalIgnoreCase)
            && approvedRecipients.Contains(recipient))
        {
            reason = $"SKIP_ALREADY_APPROVED: Recipient {recipient} already has an approved consent in IYS.";
            return true;
        }

        if (string.Equals(status, "RET", StringComparison.OrdinalIgnoreCase)
            && !approvedRecipients.Contains(recipient))
        {
            reason = $"SKIP_RET_NOT_APPROVED: Recipient {recipient} is not returned as approved by IYS.";
            return true;
        }

        var logDate = ParseConsentDate(log.ConsentDate);
        if (logDate.HasValue && logDate.Value < DateTime.UtcNow.AddDays(-3))
        {
            reason = $"SKIP_OUTDATED_CONSENT: Pending consent date {logDate:O} is older than 3 days.";
            return true;
        }

        return false;
    }

    private static DateTime? ParseConsentDate(string? consentDate)
    {
        if (string.IsNullOrWhiteSpace(consentDate))
        {
            return null;
        }

        var trimmed = consentDate.Trim();
        var formats = new[]
        {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "dd.MM.yyyy HH:mm:ss"
        };

        if (DateTime.TryParseExact(trimmed, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
        {
            return parsed;
        }

        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed))
        {
            return parsed;
        }

        if (DateTime.TryParse(trimmed, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed))
        {
            return parsed;
        }

        return null;
    }

    // IYS queryMultipleConsent çağrısını yapıp dönüşleri tek yerde topluyoruz.
    private async Task<QueryMultipleConsentInfo> GetApprovedConsentEntriesAsync(
        string companyCode,
        string recipientType,
        string? consentType,
        IReadOnlyCollection<string> recipients)
    {

        var info = new QueryMultipleConsentInfo();
        if (string.IsNullOrWhiteSpace(companyCode)
            || string.IsNullOrWhiteSpace(recipientType)
            || recipients == null
            || recipients.Count == 0)
        {
            return info;
        }

        var recipientList = recipients
            .Where(recipient => !string.IsNullOrWhiteSpace(recipient))
            .Select(recipient => recipient.Trim())
            .Where(recipient => !string.IsNullOrWhiteSpace(recipient))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipientList.Count == 0)
        {
            return info;
        }

        var request = new RecipientKeyWithList
        {
            RecipientType = recipientType,
            Type = string.IsNullOrWhiteSpace(consentType) ? null : consentType,
            Recipients = recipientList
        };

        try
        {
            var response = await _client.PostJsonAsync<RecipientKeyWithList, QueryMultipleConsentEnvelope>(
                $"consents/{companyCode}/queryMultipleConsent",
                request);

            if (response?.Data?.List is JArray array)
            {
                foreach (var token in array)
                {
                    switch (token.Type)
                    {
                        case JTokenType.String:
                            var recipient = token.Value<string>()?.Trim();
                            if (!string.IsNullOrWhiteSpace(recipient))
                            {
                                info.Entries.Add(new MultipleQueryConsentEntry
                                {
                                    Recipient = recipient!,
                                    Status = "ON"
                                });
                            }
                            break;
                        case JTokenType.Object:
                            var entry = token.ToObject<MultipleQueryConsentEntry>();
                            if (entry != null && !string.IsNullOrWhiteSpace(entry.Recipient))
                            {
                                entry.Recipient = entry.Recipient.Trim();
                                if (string.IsNullOrWhiteSpace(entry.Status))
                                {
                                    entry.Status = "ON";
                                }

                                info.Entries.Add(entry);
                            }
                            break;
                    }
                }
            }

            if (response != null)
            {
                if (response.Messages is { Count: > 0 })
                {
                    foreach (var kv in response.Messages)
                    {
                        if (!info.Messages.ContainsKey(kv.Key))
                        {
                            info.Messages[kv.Key] = kv.Value;
                        }
                    }
                }

                if (!response.IsSuccessful())
                {
                    info.Messages[$"query.error.{companyCode}"] = BuildErrorMessage(response);
                }
            }
        }
        catch (Exception ex)
        {
            info.Messages[$"query.exception.{companyCode}"] = ex.Message;
        }

        return info;
    }

    private static ConsentResponseUpdate CreateOverdueUpdate(ConsentRequestLog log, string message)
    {
        return new ConsentResponseUpdate
        {
            Id = log.Id,
            LogId = log.LogId ?? 0,
            IsSuccess = false,
            TransactionId = null,
            CreationDate = null,
            BatchError = message,
            IsOverdue = true
        };
    }

    // queryMultipleConsent yanıtındaki kayıtları ve mesajları tek noktada tutuyoruz.
    private sealed class QueryMultipleConsentInfo
    {
        public List<MultipleQueryConsentEntry> Entries { get; } = new();

        public Dictionary<string, string> Messages { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class QueryMultipleConsentEnvelope
    {
        [JsonProperty("requestId")]
        public string? RequestId { get; set; }

        [JsonProperty("list")]
        public JToken? List { get; set; }
    }

    private sealed class MultipleQueryConsentEntry
    {
        [JsonProperty("recipient")]
        public string Recipient { get; set; } = string.Empty;

        [JsonProperty("status")]
        public string? Status { get; set; }

        [JsonProperty("reason")]
        public string? Reason { get; set; }
    }

    private sealed record ConsentGroupKey(string RecipientType, string Type);

    private sealed class ConsentGroupKeyComparer : IEqualityComparer<ConsentGroupKey>
    {
        public static ConsentGroupKeyComparer Instance { get; } = new ConsentGroupKeyComparer();

        public bool Equals(ConsentGroupKey? x, ConsentGroupKey? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return string.Equals(x.RecipientType, y.RecipientType, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Type, y.Type, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(ConsentGroupKey obj)
        {
            var recipientType = obj.RecipientType?.ToUpperInvariant() ?? string.Empty;
            var type = obj.Type?.ToUpperInvariant() ?? string.Empty;
            return HashCode.Combine(recipientType, type);
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
