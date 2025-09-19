using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request;
using IYSIntegration.Application.Services.Models.Response.Consent;
using IYSIntegration.Application.Services.Models.Response.Schedule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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

                    var approvedRecipients = await GetApprovedRecipientsAsync(
                        companyCode,
                        recipientType,
                        string.IsNullOrWhiteSpace(consentType) ? null : consentType,
                        recipients);

                    foreach (var log in consentGroup)
                    {
                        if (ShouldSkipConsent(log, approvedRecipients, out var skipReason))
                        {
                            responseUpdates.Add(CreateOverdueUpdate(log, skipReason));
                            _logger.LogInformation(
                                "SingleConsentWorker skipped log {Id} for company {CompanyCode} and recipient {Recipient}: {Reason}",
                                log.Id,
                                companyCode,
                                log.Recipient,
                                skipReason);
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

                            var addResponse = await _client.PostJsonAsync<Consent, AddConsentResult>("consents/{companyCode}/addConsent", request.Consent);

                            addResponse.Id = log.Id;

                            var update = CreateConsentResponseUpdate(addResponse);
                            if (update != null)
                            {
                                responseUpdates.Add(update);
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

        if (response.HttpStatusCode == 200)
        {
            _logger.LogInformation("SingleConsentWorker ID {Id} ve {Status} statu olarak alındı", response.Id, response.HttpStatusCode);
        }
        else if (isConsentOverdue)
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

    private bool ShouldSkipConsent(ConsentRequestLog log, ISet<string> approvedRecipients, out string reason)
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
        var isApprovedInIys = approvedRecipients.Contains(recipient);

        if (string.Equals(status, "ON", StringComparison.OrdinalIgnoreCase) && isApprovedInIys)
        {
            reason = "SKIP_ALREADY_APPROVED: QueryMultipleConsent shows recipient already approved in IYS.";
            return true;
        }

        if (string.Equals(status, "RET", StringComparison.OrdinalIgnoreCase) && !isApprovedInIys)
        {
            reason = "SKIP_RET_NOT_PRESENT: Recipient missing from QueryMultipleConsent results; RET should not be sent.";
            return true;
        }

        return false;
    }

    private async Task<HashSet<string>> GetApprovedRecipientsAsync(
        string companyCode,
        string recipientType,
        string? consentType,
        IReadOnlyCollection<string> recipients)
    {
        var approved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(companyCode)
            || string.IsNullOrWhiteSpace(recipientType)
            || recipients == null
            || recipients.Count == 0)
        {
            return approved;
        }

        var recipientList = recipients
            .Where(recipient => !string.IsNullOrWhiteSpace(recipient))
            .Select(recipient => recipient.Trim())
            .Where(recipient => !string.IsNullOrWhiteSpace(recipient))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipientList.Count == 0)
        {
            return approved;
        }

        var request = new RecipientKeyWithList
        {
            RecipientType = recipientType,
            Type = string.IsNullOrWhiteSpace(consentType) ? null : consentType,
            Recipients = recipientList
        };

        try
        {
            var response = await _client.PostJsonAsync<RecipientKeyWithList, MultipleQueryConsentResult>(
                $"consents/{companyCode}/queryMultipleConsent",
                request);

            if (response?.IsSuccessful() == true && response.Data?.List != null)
            {
                foreach (var entry in response.Data.List)
                {
                    var trimmed = entry?.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        approved.Add(trimmed);
                    }
                }
            }
            else if (response != null)
            {
                _logger.LogWarning(
                    "SingleConsentWorker queryMultipleConsent for company {CompanyCode} returned HTTP {StatusCode} and status {Status}.",
                    companyCode,
                    response.HttpStatusCode,
                    response.Status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "SingleConsentWorker failed to query existing consents for company {CompanyCode}.",
                companyCode);
        }

        return approved;
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
