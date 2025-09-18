using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request.Consent;
using IYSIntegration.Application.Services.Models.Response.Consent;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            var successCount = 0;
            var failedCount = 0;

            var limit = rowCount > 0 ? rowCount : 900;
            var chunkSize = Math.Min(limit, 900);
            if (chunkSize <= 0)
            {
                chunkSize = 900;
            }

            try
            {
                var pendingRequests = await _dbService.GetPendingConsentsWithoutPull(limit);

                if (pendingRequests.Count == 0)
                {
                    response.Data = new ScheduledJobStatistics
                    {
                        SuccessCount = 0,
                        FailedCount = 0
                    };
                    return response;
                }

                var requestsByCompany = new Dictionary<string, List<ConsentRequestLog>>(StringComparer.OrdinalIgnoreCase);

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

                    if (!requestsByCompany.TryGetValue(companyCode, out var list))
                    {
                        list = new List<ConsentRequestLog>();
                        requestsByCompany[companyCode] = list;
                    }

                    list.Add(request);
                }

                foreach (var kvp in requestsByCompany)
                {
                    var companyCode = kvp.Key;
                    var requests = kvp.Value;

                    if (requests.Count == 0)
                    {
                        continue;
                    }

                    var consentParams = ResolveConsentParams(companyCode, requests);
                    if (consentParams == null)
                    {
                        results.Add(new LogResult
                        {
                            Id = 0,
                            CompanyCode = companyCode,
                            Messages = new Dictionary<string, string>
                            {
                                { "ConsentParams", "IYS kodu bulunamadı." }
                            }
                        });
                        Interlocked.Add(ref failedCount, requests.Count);
                        continue;
                    }

                    for (var index = 0; index < requests.Count; index += chunkSize)
                    {
                        var length = Math.Min(chunkSize, requests.Count - index);
                        var chunk = requests.GetRange(index, length);

                        var payload = chunk
                            .Select(r => new RecipientKey
                            {
                                Recipient = r.Recipient,
                                RecipientType = r.RecipientType,
                                Type = r.Type
                            })
                            .ToList();

                        try
                        {
                            var queryResponse = await _client.PostJsonAsync<List<RecipientKey>, MultipleConsentResult>(
                                $"consents/{companyCode}/queryMultipleConsent",
                                payload);

                            if (queryResponse.IsSuccessful()
                                && queryResponse.Data?.SubRequests != null
                                && queryResponse.Data.SubRequests.Length > 0)
                            {
                                var subRequests = queryResponse.Data.SubRequests;
                                var processedIds = new List<long>();
                                var maxLoop = Math.Min(subRequests.Length, chunk.Count);

                                for (var i = 0; i < maxLoop; i++)
                                {
                                    var subRequest = subRequests[i];
                                    var sourceConsent = chunk[i];

                                    if (!string.Equals(subRequest.Status, "success", StringComparison.OrdinalIgnoreCase))
                                    {
                                        results.Add(new LogResult
                                        {
                                            Id = sourceConsent.Id,
                                            CompanyCode = companyCode,
                                            Messages = new Dictionary<string, string>
                                            {
                                                { "QueryMultipleConsent", subRequest.Status ?? "Bilinmeyen durum" }
                                            }
                                        });
                                        Interlocked.Increment(ref failedCount);
                                        continue;
                                    }

                                    var insertRequest = new AddConsentRequest
                                    {
                                        CompanyCode = companyCode,
                                        IysCode = consentParams.IysCode,
                                        BrandCode = consentParams.BrandCode,
                                        Consent = new Consent
                                        {
                                            Recipient = subRequest.Recipient ?? sourceConsent.Recipient,
                                            Type = subRequest.Type ?? sourceConsent.Type,
                                            Source = subRequest.Source ?? sourceConsent.Source,
                                            Status = subRequest.Status,
                                            ConsentDate = subRequest.ConsentDate.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                                            RecipientType = subRequest.RecipientType ?? sourceConsent.RecipientType,
                                            TransactionId = subRequest.SubRequestId
                                        }
                                    };

                                    await _dbService.InsertPullConsent(insertRequest);
                                    processedIds.Add(sourceConsent.Id);
                                    Interlocked.Increment(ref successCount);
                                }

                                if (chunk.Count > maxLoop)
                                {
                                    for (var i = maxLoop; i < chunk.Count; i++)
                                    {
                                        var missing = chunk[i];
                                        results.Add(new LogResult
                                        {
                                            Id = missing.Id,
                                            CompanyCode = companyCode,
                                            Messages = new Dictionary<string, string>
                                            {
                                                { "QueryMultipleConsent", "Yanıt alınamadı." }
                                            }
                                        });
                                        Interlocked.Increment(ref failedCount);
                                    }
                                }

                                if (processedIds.Count > 0)
                                {
                                    try
                                    {
                                        await _dbService.MarkConsentsAsPulled(processedIds);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Failed to update IsPulled flags for company {CompanyCode}", companyCode);
                                    }
                                }
                            }
                            else
                            {
                                var errorMessage = BuildErrorMessage(queryResponse);
                                foreach (var request in chunk)
                                {
                                    results.Add(new LogResult
                                    {
                                        Id = request.Id,
                                        CompanyCode = companyCode,
                                        Messages = new Dictionary<string, string>
                                        {
                                            { "QueryMultipleConsent", errorMessage }
                                        }
                                    });
                                    Interlocked.Increment(ref failedCount);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Pending consent multi query error for company {CompanyCode}", companyCode);
                            foreach (var request in chunk)
                            {
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

        private ConsentParams? ResolveConsentParams(string companyCode, List<ConsentRequestLog> requests)
        {
            var withCodes = requests.FirstOrDefault(r => r.IysCode != 0 && r.BrandCode != 0);
            if (withCodes != null)
            {
                return new ConsentParams
                {
                    IysCode = withCodes.IysCode,
                    BrandCode = withCodes.BrandCode
                };
            }

            return _iysHelper.GetIysCode(companyCode);
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
