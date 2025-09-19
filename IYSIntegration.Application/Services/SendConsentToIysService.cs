using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request;
using IYSIntegration.Application.Services.Models.Response.Consent;
using IYSIntegration.Application.Services.Models.Response.Schedule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace IYSIntegration.Application.Services;

public class SendConsentToIysService
{
    private readonly ILogger<SendConsentToIysService> _logger;
    private readonly IDbService _dbService;
    private readonly IysProxy _client;
    private readonly IIysHelper _iysHelper;
    private readonly bool _isIysOnline;
    private readonly bool _isMetricsOnline;
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
        IysProxy client,
        IConfiguration configuration)
    {
        _logger = logger;
        _dbService = dbHelper;
        _client = client;
        _iysHelper = iysHelper;
        _isIysOnline = configuration.GetValue("IsIysOnline", true);
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
                foreach (var log in group.Value)
                {
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


                        if (!_isIysOnline)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5));
                            continue;
                        }

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

            if (!_isIysOnline)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            else if (responsesToUpdate.Length > 0)
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
