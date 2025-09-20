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
using System.Globalization;

namespace IYSIntegration.Application.Services;

public class SendConsentToIysService
{
    private readonly ILogger<SendConsentToIysService> _logger;
    private readonly IDbService _dbService;
    private readonly IIysProxy _client;
    private readonly IIysHelper _iysHelper;

    private static readonly HashSet<string> OverdueErrorCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "H174", "H175", "H178"
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
                    var recipientType = consentGroup.Key.RecipientType;
                    var consentType = consentGroup.Key.Type;

                    var recipients = consentGroup
                        .Select(log => log.Recipient)
                        .Where(recipient => !string.IsNullOrWhiteSpace(recipient))
                        .Select(recipient => recipient!.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    var approvedRecipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var latestPendingByRecipient = new Dictionary<string, ConsentRequestLog>(StringComparer.OrdinalIgnoreCase);

                    foreach (var pending in consentGroup)
                    {
                        var pendingRecipient = pending.Recipient?.Trim();
                        if (string.IsNullOrWhiteSpace(pendingRecipient)) continue;

                        if (!latestPendingByRecipient.ContainsKey(pendingRecipient))
                            latestPendingByRecipient[pendingRecipient] = pending;
                    }

                    if (consentGroup.Any(log => string.Equals(log.Status?.Trim(), "ON", StringComparison.OrdinalIgnoreCase)))
                    {
                        var queryInfo = await GetApprovedConsentEntriesAsync(
                            companyCode,
                            recipientType,
                            string.IsNullOrWhiteSpace(consentType) ? null : consentType,
                            recipients);

                        if (queryInfo.Messages.Count > 0)
                            response.AddMessage(queryInfo.Messages);

                        foreach (var entry in queryInfo.Entries)
                        {
                            var trimmedRecipient = entry?.Recipient?.Trim();
                            if (string.IsNullOrWhiteSpace(trimmedRecipient)) continue;

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
                        ConsentRequestLog? latestPending = null;
                        var trimmedRecipient = log.Recipient?.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmedRecipient))
                            latestPendingByRecipient.TryGetValue(trimmedRecipient, out latestPending);

                        if (ShouldSkipConsent(log, approvedRecipients, latestPending, out var skipReason))
                        {
                            responseUpdates.Add(CreateOverdueUpdate(log, skipReason));
                            results.Add(new LogResult
                            {
                                Id = log.Id,
                                CompanyCode = companyCode,
                                Status = "Atlandı",
                                Messages = new Dictionary<string, string>
                                {
                                    { "Neden", skipReason }
                                }
                            });
                            continue;
                        }

                        try
                        {
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

                            var addResponse = await _client.PostJsonAsync<Consent, AddConsentResult>(
                                $"consents/{companyCode}/addConsent", request.Consent);

                            addResponse.Id = log.Id;

                            var update = CreateConsentResponseUpdate(addResponse, out var addMessage);
                            if (update != null) responseUpdates.Add(update);

                            if (addMessage.HasValue)
                                response.AddMessage(addMessage.Value.Key, addMessage.Value.Value);

                            if (addResponse.IsSuccessful() && addResponse.HttpStatusCode is >= 200 and < 300)
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
                                        { "Ekleme Hatası", BuildErrorMessage(addResponse) }
                                    }
                                });
                                Interlocked.Increment(ref failedCount);
                            }
                        }
                        catch (Exception ex)
                        {
                            results.Add(new LogResult
                            {
                                Id = log.Id,
                                CompanyCode = companyCode,
                                Messages = new Dictionary<string, string> { { "Hata", ex.Message } }
                            });
                            Interlocked.Increment(ref failedCount);
                            _logger.LogError(ex, "SingleConsentAddService servisinde hata: log ID {Id}", log.Id);
                            response.Error();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SingleConsentAddService gene hata");
            results.Add(new LogResult { Id = 0, CompanyCode = "", Status = "Hata", Messages = new Dictionary<string, string> { { "Hata", ex.Message } } });
            response.Error("SingleConsentAddService", "Beklenmeyen hata");
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
                _logger.LogError(ex, "Kayıt durum güncellenirken hata");
                response.AddMessage("Veritabanı", ex.Message);
                response.Error();
            }
        }

        foreach (var result in results)
            response.AddMessage(result.GetMessages());

        response.Data = new ScheduledJobStatistics
        {
            SuccessCount = successCount,
            FailedCount = failedCount
        };

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
        if (response == null) return null;

        var errorCodeList = response.OriginalError?.Errors?
            .Select(x => x.Code)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToList() ?? new List<string>();

        var isConsentOverdue = response.OriginalError?.Errors?.Any(x =>
            !string.IsNullOrWhiteSpace(x.Code) && OverdueErrorCodes.Contains(x.Code!)) ?? false;

        var errors = errorCodeList.Count > 0 ? string.Join(",", errorCodeList) : "Mevcut olmayan";

        if (isConsentOverdue)
        {
            responseMessage = new($"overdue.{response.Id}", $"Log {response.Id} için IYS yanıtı gecikmeli veya mükerrer: {errors}.");
        }
        else
        {
            responseMessage = new($"error.{response.Id}", $"Log {response.Id} için IYS gönderimi başarısız: {BuildErrorMessage(response)}");
        }

        var serializedError = response.OriginalError == null
            ? null
            : JsonConvert.SerializeObject(response.OriginalError, Formatting.None,
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

    private bool ShouldSkipConsent(
        ConsentRequestLog log,
        ISet<string> approvedRecipients,
        ConsentRequestLog? latestPending,
        out string reason)
    {
        reason = string.Empty;
        if (log == null) return false;

        var recipient = log.Recipient?.Trim();
        var status = log.Status?.Trim();

        if (string.IsNullOrWhiteSpace(recipient) || string.IsNullOrWhiteSpace(status))
            return false;

        approvedRecipients ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (latestPending != null && latestPending.Id != log.Id)
        {
            reason = $"ESKİ BEKLEYEN KAYIT: Alıcı {recipient} için daha yeni bir bekleyen izin kaydı var (Log ID {latestPending.Id}).";
            return true;
        }

        if (string.Equals(status, "ON", StringComparison.OrdinalIgnoreCase)
            && approvedRecipients.Contains(recipient))
        {
            reason = $"ZATEN ONAYLI: Alıcı {recipient} zaten IYS üzerinde onaylı.";
            return true;
        }

        if (string.Equals(status, "RET", StringComparison.OrdinalIgnoreCase)
            && !approvedRecipients.Contains(recipient))
        {
            reason = $"RET DURUMU GEÇERSİZ: Alıcı {recipient} IYS tarafından onaylı olarak dönmedi.";
            return true;
        }

        var logDate = ParseConsentDate(log.ConsentDate);
        if (logDate.HasValue && logDate.Value < DateTime.UtcNow.AddDays(-3))
        {
            reason = $"EKSİK TARİH: Bekleyen izin tarihi {logDate:O}, 3 günden daha eski.";
            return true;
        }


        return false;
    }

    private static DateTime? ParseConsentDate(string? consentDate)
    {
        if (string.IsNullOrWhiteSpace(consentDate)) return null;

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
            return parsed;

        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed))
            return parsed;

        if (DateTime.TryParse(trimmed, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed))
            return parsed;

        return null;
    }

    private async Task<QueryMultipleConsentInfo> GetApprovedConsentEntriesAsync(
        string companyCode,
        string recipientType,
        string? consentType,
        IReadOnlyCollection<string> recipients)
    {
        var info = new QueryMultipleConsentInfo();

        if (string.IsNullOrWhiteSpace(companyCode) || string.IsNullOrWhiteSpace(recipientType) || recipients.Count == 0)
            return info;

        var recipientList = recipients
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipientList.Count == 0) return info;

        var request = new RecipientKeyWithList
        {
            RecipientType = recipientType,
            Type = string.IsNullOrWhiteSpace(consentType) ? null : consentType,
            Recipients = recipientList
        };

        try
        {
            var response = await _client.PostJsonAsync<RecipientKeyWithList, QueryMultipleConsentEnvelope>(
                $"consents/{companyCode}/queryMultipleConsent", request);

            if (response?.Data?.List is JArray array)
            {
                foreach (var token in array)
                {
                    switch (token.Type)
                    {
                        case JTokenType.String:
                            var recipient = token.Value<string>()?.Trim();
                            if (!string.IsNullOrWhiteSpace(recipient))
                                info.Entries.Add(new MultipleQueryConsentEntry { Recipient = recipient!, Status = "ON" });
                            break;

                        case JTokenType.Object:
                            var entry = token.ToObject<MultipleQueryConsentEntry>();
                            if (entry != null && !string.IsNullOrWhiteSpace(entry.Recipient))
                            {
                                entry.Recipient = entry.Recipient.Trim();
                                entry.Status ??= "ON";
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
                        if (!info.Messages.ContainsKey(kv.Key)) info.Messages[kv.Key] = kv.Value;
                }

                if (!response.IsSuccessful())
                    info.Messages[$"query.error.{companyCode}"] = BuildErrorMessage(response);
            }
        }
        catch (Exception ex)
        {
            info.Messages[$"query.exception.{companyCode}"] = ex.Message;
        }

        return info;
    }

    private static ConsentResponseUpdate CreateOverdueUpdate(ConsentRequestLog log, string message) =>
        new()
        {
            Id = log.Id,
            LogId = log.LogId ?? 0,
            IsSuccess = false,
            TransactionId = null,
            CreationDate = null,
            BatchError = message,
            IsOverdue = true
        };

    private sealed class QueryMultipleConsentInfo
    {
        public List<MultipleQueryConsentEntry> Entries { get; } = new();
        public Dictionary<string, string> Messages { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class QueryMultipleConsentEnvelope
    {
        [JsonProperty("requestId")] public string? RequestId { get; set; }
        [JsonProperty("list")] public JToken? List { get; set; }
    }

    private sealed class MultipleQueryConsentEntry
    {
        [JsonProperty("recipient")] public string Recipient { get; set; } = string.Empty;
        [JsonProperty("status")] public string? Status { get; set; }
        [JsonProperty("reason")] public string? Reason { get; set; }
    }

    private sealed record ConsentGroupKey(string RecipientType, string Type);

    private sealed class ConsentGroupKeyComparer : IEqualityComparer<ConsentGroupKey>
    {
        public static ConsentGroupKeyComparer Instance { get; } = new();

        public bool Equals(ConsentGroupKey? x, ConsentGroupKey? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return string.Equals(x.RecipientType, y.RecipientType, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Type, y.Type, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(ConsentGroupKey obj) =>
            HashCode.Combine(obj.RecipientType?.ToUpperInvariant() ?? string.Empty,
                             obj.Type?.ToUpperInvariant() ?? string.Empty);
    }

    private static string BuildErrorMessage<T>(ResponseBase<T> response)
    {
        var parts = new List<string>();

        if (response.Messages is { Count: > 0 })
            parts.AddRange(response.Messages.Select(kv => $"{kv.Key}: {kv.Value}"));

        if (!string.IsNullOrWhiteSpace(response.OriginalError?.Message))
            parts.Add($"Message: {response.OriginalError.Message}");

        if (response.OriginalError?.Errors != null && response.OriginalError.Errors.Length > 0)
        {
            parts.AddRange(response.OriginalError.Errors
                .Where(e => !string.IsNullOrWhiteSpace(e.Code) || !string.IsNullOrWhiteSpace(e.Message))
                .Select(e => string.IsNullOrWhiteSpace(e.Code)
                    ? e.Message ?? string.Empty
                    : string.IsNullOrWhiteSpace(e.Message) ? e.Code : $"{e.Code}: {e.Message}"));
        }

        return string.Join(" | ", parts);
    }
}
