using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request;
using IYSIntegration.Application.Services.Models.Response.Consent;
using IYSIntegration.Application.Services.Models.Response.Schedule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;

namespace IYSIntegration.Application.Services;

public class SendConsentToIysService
{
    private readonly ILogger<SendConsentToIysService> _logger;
    private readonly IDbService _dbService;
    private readonly IIysProxy _client;
    private readonly IIysHelper _iysHelper;

    private static readonly HashSet<string> ApprovalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
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

        _logger.LogInformation("SingleConsentAddService started at {Time}", DateTimeOffset.Now);

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
                                { "Atlandı", skipReason }
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
                        var request = new AddConsentRequest
                        {
                            IysCode = log.IysCode,
                            BrandCode = log.BrandCode,
                            CompanyCode = group.Key,
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

                        var addResponse = await _client.PostJsonAsync<Consent, AddConsentResult>($"consents/{group.Key}/addConsent", request.Consent);

                        addResponse.Id = log.Id;

                        var update = CreateConsentResponseUpdate(addResponse);
                        if (update != null)
                        {
                            responseUpdates.Add(update);
                        }

                        if (addResponse.IsSuccessful() && addResponse.HttpStatusCode >= 200 && addResponse.HttpStatusCode < 300)
                        {
                            Interlocked.Increment(ref successCount);
                            var normalizedRecipient = NormalizeRecipient(log.Recipient);
                            if (!string.IsNullOrWhiteSpace(normalizedRecipient))
                            {
                                queryInfo.Recipients.Add(normalizedRecipient);
                            }
                        }
                        else
                        {
                            results.Add(new LogResult
                            {
                                Id = log.Id,
                                CompanyCode = group.Key,
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
                        results.Add(new LogResult { Id = log.Id, CompanyCode = group.Key, Messages = new Dictionary<string, string> { { "Exception", ex.Message } } });
                        Interlocked.Increment(ref failedCount);
                        _logger.LogError(ex, "Exception in SingleConsentAddService for log ID {Id}", log.Id);
                        response.Error();
                    }

                    queryCache[cacheKey] = queryInfo;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in SingleConsentAddService");
            results.Add(new LogResult { Id = 0, CompanyCode = "", Status = "Failed", Messages = new Dictionary<string, string> { { "Exception", ex.Message } } });
            response.Error("SingleConsentAddService", "Bilinmeyen hata.");
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
                _logger.LogError(ex, "İşlem sonrası güncelleme yapılamadı.");
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

            //ARAMA, MESAJ ve EPOSTA izinleri için istek gövdesinde bulunan iletişim adreslerinden hangilerinin ONAY durumunda olduğu bilgisi dönülür.
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
                "queryMultipleConsent {CompanyCode} ({RecipientType}/{Type}) için hata verdi {Status}",
                companyCode,
                recipientType,
                consentType,
                response.HttpStatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Mevcut rızaları sorgularken hata: {CompanyCode} ({RecipientType}/{Type})",
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
            return "IYS'de olan onay rıza içingönderim yapılmadı.";
        }

        if (IsRejectionStatus(status) && !existsInList)
        {
            return "IYS'de olmayan rıza için ret gönderilmedi.";
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

        if (isConsentOverdue)
        {
            _logger.LogWarning("SingleConsentWorker ID {Id} ve IYS geciken/mükerrer {Errors} olarak alındı", response.Id, errors);
        }
        else
        {
            _logger.LogError("SingleConsentWorker ID {Id} ve {Status} statu kodu ve {Errors} IYS hataları ile alınamadı", response.Id, response.HttpStatusCode, errors);
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

        return string.Join(" | ", parts);
    }
}
