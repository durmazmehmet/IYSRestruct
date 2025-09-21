using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IYS.Application.Services.Interface;
using IYS.Application.Services.Models.Base;
using IYS.Application.Services.Models.Request;
using IYS.Application.Services.Models.Response.Consent;
using IYS.Application.Services.Models.Response.Schedule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace IYS.Application.Services;

public sealed class SendMultipleConsentToIysService
{
    private const int MaxConsentPerBatch = 100;
    private static readonly Regex PhoneNumberRegex = new("^\\+905\\d{9}$", RegexOptions.Compiled);
    private static readonly HashSet<string> PhoneConsentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "MESAJ",
        "ARAMA"
    };

    private readonly ILogger<SendMultipleConsentToIysService> _logger;
    private readonly IDbService _dbService;
    private readonly IIysProxy _client;
    private readonly IIysHelper _iysHelper;
    private readonly TimeSpan _statusQueryDelay;

    private sealed record ConsentBatch(string CompanyCode, IReadOnlyList<ConsentRequestLog> Consents);

    public SendMultipleConsentToIysService(
        ILogger<SendMultipleConsentToIysService> logger,
        IDbService dbService,
        IIysHelper iysHelper,
        IIysProxy client,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        _iysHelper = iysHelper ?? throw new ArgumentNullException(nameof(iysHelper));
        _client = client ?? throw new ArgumentNullException(nameof(client));

        var delaySeconds = configuration?.GetValue<int?>("MultipleConsentQueryDelay") ?? 300;
        if (delaySeconds < 0)
        {
            delaySeconds = 300;
        }

        _statusQueryDelay = TimeSpan.FromSeconds(delaySeconds);
    }

    public async Task<ResponseBase<ScheduledJobStatistics>> SendPendingBatchesAsync(int batchCount)
    {
        var response = new ResponseBase<ScheduledJobStatistics>();
        response.Success();

        if (batchCount <= 0)
        {
            response.AddMessage("Info", "İşlenecek batch sayısı belirtilmedi.");
            response.Data = new ScheduledJobStatistics { SuccessCount = 0, FailedCount = 0 };
            return response;
        }

        var maxRowCount = Math.Max(batchCount * MaxConsentPerBatch, MaxConsentPerBatch);

        try
        {
            var pendingConsents = await _dbService.GetPendingMultipleConsentsAsync(maxRowCount);
            if (pendingConsents.Count == 0)
            {
                response.AddMessage("Info", "Gönderilecek bekleyen rıza bulunamadı.");
                response.Data = new ScheduledJobStatistics { SuccessCount = 0, FailedCount = 0 };
                return response;
            }

            var failureUpdates = new List<ConsentResponseUpdate>();
            var results = new List<LogResult>();
            var groupedConsents = new Dictionary<string, List<ConsentRequestLog>>(StringComparer.OrdinalIgnoreCase);

            foreach (var consent in pendingConsents)
            {
                var resolvedCompanyCode = _iysHelper.ResolveCompanyCode(consent.CompanyCode, consent.IysCode) ?? consent.CompanyCode;

                if (string.IsNullOrWhiteSpace(resolvedCompanyCode))
                {
                    failureUpdates.Add(CreateFailureUpdate(consent.Id, 0, "Şirket kodu bulunamadı.", false, null));
                    results.Add(new LogResult
                    {
                        Id = consent.Id,
                        CompanyCode = consent.CompanyCode ?? string.Empty,
                        Status = "Skipped",
                        Messages = new Dictionary<string, string> { { "Company", "Şirket kodu bulunamadı." } }
                    });
                    continue;
                }

                consent.CompanyCode = resolvedCompanyCode.Trim();

                if (!groupedConsents.TryGetValue(consent.CompanyCode, out var companyConsents))
                {
                    companyConsents = new List<ConsentRequestLog>();
                    groupedConsents.Add(consent.CompanyCode, companyConsents);
                }

                companyConsents.Add(consent);
            }

            if (failureUpdates.Count > 0)
            {
                await _dbService.UpdateConsentResponses(failureUpdates);
            }

            var batches = BuildBatches(groupedConsents, batchCount);
            var pendingUpdates = new List<ConsentResponseUpdate>();
            var validationFailures = new List<ConsentResponseUpdate>();
            int successCount = 0;
            int failedCount = failureUpdates.Count;

            foreach (var batch in batches)
            {
                var orderedConsents = batch.Consents
                    .OrderBy(c => c.CreateDate ?? DateTime.MaxValue)
                    .ThenBy(c => c.Id)
                    .ToList();

                var validConsents = new List<ConsentRequestLog>();
                var duplicateTracker = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var consent in orderedConsents)
                {
                    var validationMessage = ValidateConsent(consent, duplicateTracker);
                    if (validationMessage != null)
                    {
                        validationFailures.Add(CreateFailureUpdate(consent.Id, 0, validationMessage, false, null));
                        failedCount++;
                        results.Add(new LogResult
                        {
                            Id = consent.Id,
                            CompanyCode = batch.CompanyCode,
                            Status = "Skipped",
                            Messages = new Dictionary<string, string> { { "Validation", validationMessage } }
                        });
                        continue;
                    }

                    validConsents.Add(consent);
                }

                if (validConsents.Count == 0)
                {
                    continue;
                }

                var requestPayload = validConsents
                    .Select(consent => new Consent
                    {
                        ConsentDate = consent.ConsentDate,
                        Source = consent.Source,
                        Recipient = consent.Recipient,
                        RecipientType = consent.RecipientType,
                        Status = consent.Status,
                        Type = consent.Type,
                        RetailerCode = consent.RetailerCode,
                        RetailerAccess = consent.RetailerAccess
                    })
                    .ToList();

                try
                {
                    var addResponse = await _client.PostJsonAsync<List<Consent>, MultipleConsentResult>(
                        $"consents/{batch.CompanyCode}/addMultipleConsent",
                        requestPayload);

                    if (addResponse.IsSuccessful() && addResponse.HttpStatusCode >= 200 && addResponse.HttpStatusCode < 300)
                    {
                        var requestId = addResponse.Data?.RequestId;
                        long? batchId = null;

                        if (!string.IsNullOrWhiteSpace(requestId) && long.TryParse(requestId, out var parsedRequestId))
                        {
                            batchId = parsedRequestId;
                        }
                        else if (!string.IsNullOrWhiteSpace(requestId))
                        {
                            _logger.LogWarning(
                                "Çoklu izin ekleme isteği için requestId değeri sayıya çevrilemedi. Batch güncellemesi yapılırken requestId bilgisi BatchError alanına yazılacak. RequestId: {RequestId}",
                                requestId);
                        }

                        foreach (var consent in validConsents)
                        {
                            pendingUpdates.Add(new ConsentResponseUpdate
                            {
                                Id = consent.Id,
                                LogId = addResponse.LogId,
                                BatchId = batchId,
                                IsSuccess = false,
                                TransactionId = null,
                                CreationDate = null,
                                BatchError = batchId.HasValue ? null : $"RequestId alınamadı: {requestId}",
                                IsOverdue = false
                            });
                        }

                        successCount += validConsents.Count;

                        results.Add(new LogResult
                        {
                            Id = validConsents.Last().Id,
                            CompanyCode = batch.CompanyCode,
                            Status = "Queued",
                            Messages = new Dictionary<string, string>
                            {
                                { "Info", batchId.HasValue ? $"{validConsents.Count} kayıt IYS kuyruğuna gönderildi." : $"{validConsents.Count} kayıt gönderildi ancak requestId alınamadı." }
                            }
                        });
                    }
                    else
                    {
                        failedCount += validConsents.Count;
                        var errorMessage = BuildErrorMessage(addResponse);
                        results.Add(new LogResult
                        {
                            Id = validConsents.Last().Id,
                            CompanyCode = batch.CompanyCode,
                            Status = "Failed",
                            Messages = new Dictionary<string, string> { { "Error", errorMessage } }
                        });
                        _logger.LogError(
                            "Çoklu izin ekleme isteği başarısız oldu. Company: {CompanyCode}, HttpStatus: {Status}, Error: {Error}",
                            batch.CompanyCode,
                            addResponse.HttpStatusCode,
                            errorMessage);
                    }
                }
                catch (Exception ex)
                {
                    failedCount += validConsents.Count;
                    results.Add(new LogResult
                    {
                        Id = validConsents.Last().Id,
                        CompanyCode = batch.CompanyCode,
                        Status = "Exception",
                        Messages = new Dictionary<string, string> { { "Exception", ex.Message } }
                    });
                    _logger.LogError(ex, "Çoklu izin ekleme isteği sırasında hata oluştu. Company: {CompanyCode}", batch.CompanyCode);
                }
            }

            if (validationFailures.Count > 0)
            {
                await _dbService.UpdateConsentResponses(validationFailures);
            }

            if (pendingUpdates.Count > 0)
            {
                await _dbService.UpdateConsentResponses(pendingUpdates);
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Çoklu izin ekleme servisinde beklenmeyen hata oluştu.");
            response.Error("SendMultipleConsent", "Çoklu izin ekleme sırasında beklenmeyen hata oluştu.");
        }

        return response;
    }

    public async Task<ResponseBase<ScheduledJobStatistics>> QueryPendingBatchesAsync(int batchCount = 1)
    {
        var response = new ResponseBase<ScheduledJobStatistics>();
        response.Success();

        try
        {
            var batchIds = await _dbService.GetPendingBatchIdsAsync(batchCount, _statusQueryDelay);

            if (batchIds.Count == 0)
            {
                response.AddMessage("Info", "Sorgulanacak uygun batch bulunamadı.");
                response.Data = new ScheduledJobStatistics { SuccessCount = 0, FailedCount = 0 };
                return response;
            }

            var updates = new List<ConsentResponseUpdate>();
            var results = new List<LogResult>();
            int successCount = 0;
            int failedCount = 0;

            foreach (var batchId in batchIds)
            {
                var consents = await _dbService.GetConsentsByBatchIdAsync(batchId);
                if (consents.Count == 0)
                {
                    continue;
                }

                var first = consents.First();
                var companyCode = _iysHelper.ResolveCompanyCode(first.CompanyCode, first.IysCode) ?? first.CompanyCode;

                if (string.IsNullOrWhiteSpace(companyCode))
                {
                    results.Add(new LogResult
                    {
                        Id = batchId,
                        CompanyCode = first.CompanyCode ?? string.Empty,
                        Status = "Skipped",
                        Messages = new Dictionary<string, string> { { "Company", "Batch için geçerli şirket kodu bulunamadı." } }
                    });
                    continue;
                }

                try
                {
                    var statusResponse = await _client.GetAsync<MultipleConsentRequestStatusResult>(
                        $"consents/{companyCode}/queryMultipleConsentRequest/{batchId}");

                    if (!statusResponse.IsSuccessful() || statusResponse.HttpStatusCode < 200 || statusResponse.HttpStatusCode >= 300)
                    {
                        var errorMessage = BuildErrorMessage(statusResponse);
                        results.Add(new LogResult
                        {
                            Id = batchId,
                            CompanyCode = companyCode,
                            Status = "Failed",
                            Messages = new Dictionary<string, string> { { "Error", errorMessage } }
                        });
                        _logger.LogWarning(
                            "Batch {BatchId} için çoklu izin sorgusu başarısız oldu. HttpStatus: {Status}.",
                            batchId,
                            statusResponse.HttpStatusCode);
                        continue;
                    }

                    var subRequests = statusResponse.Data?.SubRequests;
                    if (subRequests == null || subRequests.Length == 0)
                    {
                        continue;
                    }

                    var orderedConsents = consents
                        .OrderBy(c => c.Id)
                        .ToList();

                    foreach (var sub in subRequests)
                    {
                        if (sub?.Index is null)
                        {
                            continue;
                        }

                        var index = sub.Index.Value;
                        if (index < 0 || index >= orderedConsents.Count)
                        {
                            _logger.LogWarning("Batch {BatchId} için gelen index değeri ({Index}) aralık dışında.", batchId, index);
                            continue;
                        }

                        var consent = orderedConsents[index];
                        var status = sub.Status?.Trim();

                        if (string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
                        {
                            updates.Add(new ConsentResponseUpdate
                            {
                                Id = consent.Id,
                                LogId = statusResponse.LogId,
                                BatchId = batchId,
                                IsSuccess = true,
                                TransactionId = sub.TransactionId,
                                CreationDate = sub.CreationDate,
                                BatchError = null,
                                IsOverdue = false
                            });
                            successCount++;
                            results.Add(new LogResult
                            {
                                Id = consent.Id,
                                CompanyCode = companyCode,
                                Status = "Success",
                                Messages = new Dictionary<string, string> { { "Success", "İzin IYS tarafından başarıyla işlendi." } }
                            });
                        }
                        else if (string.Equals(status, "failure", StringComparison.OrdinalIgnoreCase))
                        {
                            var errorMessage = BuildStatusErrorMessage(sub);
                            updates.Add(new ConsentResponseUpdate
                            {
                                Id = consent.Id,
                                LogId = statusResponse.LogId,
                                BatchId = batchId,
                                IsSuccess = false,
                                TransactionId = null,
                                CreationDate = null,
                                BatchError = errorMessage,
                                IsOverdue = false
                            });
                            failedCount++;
                            results.Add(new LogResult
                            {
                                Id = consent.Id,
                                CompanyCode = companyCode,
                                Status = "Failed",
                                Messages = new Dictionary<string, string> { { "Error", errorMessage } }
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Batch {BatchId} için çoklu izin sorgusu sırasında hata oluştu.", batchId);
                    results.Add(new LogResult
                    {
                        Id = batchId,
                        CompanyCode = companyCode ?? string.Empty,
                        Status = "Exception",
                        Messages = new Dictionary<string, string> { { "Exception", ex.Message } }
                    });
                }
            }

            if (updates.Count > 0)
            {
                await _dbService.UpdateConsentResponses(updates);
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Çoklu izin sorgulama servisinde beklenmeyen hata oluştu.");
            response.Error("QueryMultipleConsent", "Çoklu izin sorgulama sırasında beklenmeyen hata oluştu.");
        }

        return response;
    }

    public async Task<ResponseBase<MultipleConsentResult>> SendFromRequestAsync(MultipleConsentRequest request)
    {
        var validationMessages = new List<KeyValuePair<string, string>>();
        var hasValidationError = false;

        if (request == null)
        {
            var errorResponse = new ResponseBase<MultipleConsentResult>();
            errorResponse.Error("REQUEST_REQUIRED", "Geçerli bir istek gönderilmelidir.");
            return errorResponse;
        }

        if (request.Consents == null || request.Consents.Count == 0)
        {
            var errorResponse = new ResponseBase<MultipleConsentResult>();
            errorResponse.Error("CONSENTS_REQUIRED", "Gönderilecek en az bir rıza bulunmalıdır.");
            return errorResponse;
        }

        var companyCode = _iysHelper.ResolveCompanyCode(request.CompanyCode, request.IysCode) ?? request.CompanyCode;
        if (string.IsNullOrWhiteSpace(companyCode))
        {
            var errorResponse = new ResponseBase<MultipleConsentResult>();
            errorResponse.Error("COMPANY_REQUIRED", "İşlem için geçerli bir şirket kodu belirtilmelidir.");
            return errorResponse;
        }

        var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var validConsents = new List<Consent>();

        for (var i = 0; i < request.Consents.Count; i++)
        {
            var consent = request.Consents[i];
            if (consent == null)
            {
                hasValidationError = true;
                validationMessages.Add(new KeyValuePair<string, string>($"Consent_{i + 1}", "Consent bilgisi zorunludur."));
                continue;
            }

            var normalizedRecipient = NormalizeRecipient(consent.Recipient);
            if (string.IsNullOrWhiteSpace(normalizedRecipient))
            {
                hasValidationError = true;
                validationMessages.Add(new KeyValuePair<string, string>($"Consent_{i + 1}", "Alıcı bilgisi zorunludur."));
                continue;
            }

            if (IsPhoneConsentType(consent.Type) && !IsValidPhoneNumber(normalizedRecipient))
            {
                hasValidationError = true;
                validationMessages.Add(new KeyValuePair<string, string>($"Consent_{i + 1}", "Telefon numarası +905XXXXXXXXX formatında olmalıdır."));
                continue;
            }

            var duplicateKey = BuildDuplicateKey(consent.Recipient, consent.Type, consent.ConsentDate);
            if (!string.IsNullOrEmpty(duplicateKey) && !duplicates.Add(duplicateKey))
            {
                hasValidationError = true;
                validationMessages.Add(new KeyValuePair<string, string>($"Consent_{i + 1}", "Aynı saniyede aynı iletişim adresi ve izin türü için birden fazla kayıt gönderilemez."));
                continue;
            }

            validConsents.Add(new Consent
            {
                ConsentDate = consent.ConsentDate,
                Source = consent.Source,
                Recipient = normalizedRecipient,
                RecipientType = consent.RecipientType,
                Status = consent.Status,
                Type = consent.Type,
                RetailerCode = consent.RetailerCode,
                RetailerAccess = consent.RetailerAccess
            });
        }

        if (validConsents.Count == 0)
        {
            var errorResponse = new ResponseBase<MultipleConsentResult>();
            if (hasValidationError)
            {
                foreach (var message in validationMessages)
                {
                    errorResponse.AddMessage(message.Key, message.Value);
                }
            }
            else
            {
                errorResponse.Error("VALID_CONSENT_NOT_FOUND", "Gönderilecek geçerli rıza bulunamadı.");
            }

            return errorResponse;
        }

        var sendResponse = await _client.PostJsonAsync<List<Consent>, MultipleConsentResult>(
            $"consents/{companyCode}/addMultipleConsent",
            validConsents);

        if (hasValidationError)
        {
            foreach (var message in validationMessages)
            {
                sendResponse.AddMessage(message.Key, message.Value);
            }
        }

        return sendResponse;
    }

    public async Task<ResponseBase<MultipleConsentRequestStatusResult>> QueryStatusFromRequestAsync(string companyCode, long batchId = 0)
    {
        var response = new ResponseBase<MultipleConsentRequestStatusResult>();
        response.Success();


        if (batchId <= 0)
        {
            response.Error("BATCH_ID_REQUIRED", "Sorgulamak için geçerli bir BatchId belirtilmelidir.");
            return response;
        }

        if (string.IsNullOrWhiteSpace(companyCode))
        {
            response.Error("COMPANY_REQUIRED", "İşlem için geçerli bir şirket kodu belirtilmelidir.");
            return response;
        }

        return await _client.GetAsync<MultipleConsentRequestStatusResult>(
            $"consents/{companyCode}/queryMultipleConsentRequest/{batchId}");
    }

    private static List<ConsentBatch> BuildBatches(
        Dictionary<string, List<ConsentRequestLog>> groupedConsents,
        int desiredBatchCount)
    {
        var batches = new List<ConsentBatch>();

        foreach (var pair in groupedConsents)
        {
            var ordered = pair.Value
                .OrderBy(c => c.CreateDate ?? DateTime.MaxValue)
                .ThenBy(c => c.Id)
                .ToList();

            var index = 0;
            while (index < ordered.Count && batches.Count < desiredBatchCount)
            {
                var slice = ordered.Skip(index).Take(MaxConsentPerBatch).ToList();
                if (slice.Count == 0)
                {
                    break;
                }

                batches.Add(new ConsentBatch(pair.Key, slice));
                index += slice.Count;
            }

            if (batches.Count >= desiredBatchCount)
            {
                break;
            }
        }

        return batches;
    }

    private static ConsentResponseUpdate CreateFailureUpdate(long consentId, long logId, string message, bool isOverdue, long? batchId)
        => new()
        {
            Id = consentId,
            LogId = logId,
            BatchId = batchId,
            IsSuccess = false,
            TransactionId = null,
            CreationDate = null,
            BatchError = string.IsNullOrWhiteSpace(message) ? "Unknown error" : message,
            IsOverdue = isOverdue
        };

    private static string? ValidateConsent(ConsentRequestLog consent, HashSet<string> duplicateTracker)
    {
        var recipient = NormalizeRecipient(consent.Recipient);
        if (string.IsNullOrWhiteSpace(recipient))
        {
            return "Alıcı bilgisi zorunludur.";
        }

        if (IsPhoneConsentType(consent.Type) && !IsValidPhoneNumber(recipient))
        {
            return "Telefon numarası +905XXXXXXXXX formatında olmalıdır.";
        }

        var duplicateKey = BuildDuplicateKey(consent.Recipient, consent.Type, consent.ConsentDate);
        if (!string.IsNullOrEmpty(duplicateKey) && !duplicateTracker.Add(duplicateKey))
        {
            return "Aynı saniyede aynı iletişim adresi ve izin türü için birden fazla kayıt gönderilemez.";
        }

        return null;
    }

    private static string? NormalizeRecipient(string? recipient)
        => string.IsNullOrWhiteSpace(recipient) ? null : recipient.Trim();

    private static bool IsPhoneConsentType(string? type)
        => !string.IsNullOrWhiteSpace(type) && PhoneConsentTypes.Contains(type.Trim());

    private static bool IsValidPhoneNumber(string recipient)
        => PhoneNumberRegex.IsMatch(recipient);

    private static string BuildDuplicateKey(string? recipient, string? type, string? consentDate)
    {
        var normalizedRecipient = NormalizeRecipient(recipient);
        if (string.IsNullOrWhiteSpace(normalizedRecipient) || string.IsNullOrWhiteSpace(type))
        {
            return string.Empty;
        }

        var normalizedType = type.Trim().ToUpperInvariant();
        var normalizedDate = NormalizeConsentDate(consentDate);

        if (string.IsNullOrEmpty(normalizedDate))
        {
            return string.Empty;
        }

        return $"{normalizedRecipient}|{normalizedType}|{normalizedDate}";
    }

    private static string NormalizeConsentDate(string? consentDate)
    {
        if (string.IsNullOrWhiteSpace(consentDate))
        {
            return string.Empty;
        }

        if (DateTimeOffset.TryParse(
                consentDate,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return parsed.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        }

        return consentDate.Trim();
    }

    private static string BuildStatusErrorMessage(MultipleConsentRequestStatusSubRequest subRequest)
    {
        if (subRequest?.Error == null)
        {
            return "IYS tarafından başarısız olarak işaretlendi.";
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(subRequest.Error.Code))
        {
            parts.Add(subRequest.Error.Code.Trim());
        }

        if (!string.IsNullOrWhiteSpace(subRequest.Error.Message))
        {
            parts.Add(subRequest.Error.Message.Trim());
        }

        if (parts.Count == 0)
        {
            return JsonConvert.SerializeObject(subRequest.Error);
        }

        return string.Join(" - ", parts);
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
            parts.Add(response.OriginalError.Message);
        }

        if (response.OriginalError?.Errors != null && response.OriginalError.Errors.Length > 0)
        {
            foreach (var error in response.OriginalError.Errors)
            {
                if (!string.IsNullOrWhiteSpace(error.Code) || !string.IsNullOrWhiteSpace(error.Message))
                {
                    var code = string.IsNullOrWhiteSpace(error.Code) ? string.Empty : error.Code;
                    var message = string.IsNullOrWhiteSpace(error.Message) ? string.Empty : error.Message;
                    parts.Add(string.IsNullOrWhiteSpace(code)
                        ? message
                        : string.IsNullOrWhiteSpace(message)
                            ? code
                            : $"{code}: {message}");
                }
            }
        }

        if (parts.Count == 0)
        {
            return $"HTTP {response.HttpStatusCode}";
        }

        return string.Join(" | ", parts);
    }
}
