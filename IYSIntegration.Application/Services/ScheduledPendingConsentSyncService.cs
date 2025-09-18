using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request.Consent;
using IYSIntegration.Application.Services.Models.Response.Consent;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IYSIntegration.Application.Services
{
    public class ScheduledPendingConsentSyncService
    {
        private readonly ILogger<ScheduledPendingConsentSyncService> _logger;
        private readonly IDbService _dbService;
        private readonly IysProxy _client;
        private readonly IIysHelper _iysHelper;

        public ScheduledPendingConsentSyncService(
            ILogger<ScheduledPendingConsentSyncService> logger,
            IDbService dbService,
            IysProxy client,
            IIysHelper iysHelper)
        {
            _logger = logger;
            _dbService = dbService;
            _client = client;
            _iysHelper = iysHelper;
        }

        public async Task<ResponseBase<ScheduledJobStatistics>> RunAsync(int rowCount)
        {
            var response = new ResponseBase<ScheduledJobStatistics>();
            response.Success();

            var results = new ConcurrentBag<LogResult>();
            int successCount = 0;
            int failedCount = 0;

            try
            {
                var pendingRequests = await _dbService.GetPendingConsentsWithoutPull(rowCount);
                var processedRecipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var consentParamsCache = new Dictionary<string, ConsentParams>(StringComparer.OrdinalIgnoreCase);

                foreach (var request in pendingRequests)
                {
                    var companyCode = !string.IsNullOrWhiteSpace(request.CompanyCode)
                        ? request.CompanyCode
                        : _iysHelper.GetCompanyCode(request.IysCode) ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(companyCode))
                    {
                        results.Add(new LogResult
                        {
                            Id = request.Id,
                            CompanyCode = string.Empty,
                            Messages = new Dictionary<string, string>
                            {
                                { "Company", "Şirket kodu bulunamadı." }
                            }
                        });
                        Interlocked.Increment(ref failedCount);
                        continue;
                    }

                    var recipientKey = $"{companyCode}|{request.Recipient}|{request.RecipientType ?? string.Empty}";
                    if (!processedRecipients.Add(recipientKey))
                    {
                        continue;
                    }

                    try
                    {
                        var queryRequest = new RecipientKey
                        {
                            Recipient = request.Recipient,
                            RecipientType = request.RecipientType,
                            Type = request.Type
                        };

                        var queryResponse = await _client.PostJsonAsync<RecipientKey, QueryConsentResult>(
                            $"consents/{companyCode}/queryConsent",
                            queryRequest);

                        if (queryResponse.IsSuccessful()
                            && queryResponse.Data != null
                            && !string.IsNullOrWhiteSpace(queryResponse.Data.ConsentDate))
                        {
                            if (!consentParamsCache.TryGetValue(companyCode, out var consentParams))
                            {
                                consentParams = request.IysCode != 0 && request.BrandCode != 0
                                    ? new ConsentParams { IysCode = request.IysCode, BrandCode = request.BrandCode }
                                    : _iysHelper.GetIysCode(companyCode);
                                consentParamsCache[companyCode] = consentParams;
                            }

                            var insertRequest = new AddConsentRequest
                            {
                                CompanyCode = companyCode,
                                IysCode = consentParams.IysCode,
                                BrandCode = consentParams.BrandCode,
                                Consent = new Consent
                                {
                                    Recipient = queryResponse.Data.Recipient,
                                    Type = queryResponse.Data.Type,
                                    Source = queryResponse.Data.Source,
                                    Status = queryResponse.Data.Status,
                                    ConsentDate = queryResponse.Data.ConsentDate,
                                    RecipientType = queryResponse.Data.RecipientType,
                                    CreationDate = queryResponse.Data.CreationDate,
                                    TransactionId = queryResponse.Data.TransactionId
                                }
                            };

                            await _dbService.InsertPullConsent(insertRequest);
                            Interlocked.Increment(ref successCount);
                        }
                        else
                        {
                            results.Add(new LogResult
                            {
                                Id = request.Id,
                                CompanyCode = companyCode,
                                Messages = new Dictionary<string, string>
                                {
                                    { "Query Error", BuildErrorMessage(queryResponse) }
                                }
                            });
                            Interlocked.Increment(ref failedCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Pending consent sync error for recipient {Recipient}", request.Recipient);
                        results.Add(new LogResult
                        {
                            Id = request.Id,
                            CompanyCode = companyCode,
                            Messages = new Dictionary<string, string>
                            {
                                { "Exception", ex.Message }
                            }
                        });
                        Interlocked.Increment(ref failedCount);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in ScheduledPendingConsentSyncService");
                results.Add(new LogResult
                {
                    Id = 0,
                    CompanyCode = string.Empty,
                    Messages = new Dictionary<string, string>
                    {
                        { "Exception", ex.Message }
                    }
                });
                response.Error("PENDING_CONSENT_SYNC_FAILED", "Servis beklenmeyen bir hata nedeniyle sonlandı.");
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
}
