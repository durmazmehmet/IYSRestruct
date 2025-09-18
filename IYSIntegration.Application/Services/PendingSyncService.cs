using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request.Consent;
using IYSIntegration.Application.Services.Models.Response.Consent;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IYSIntegration.Application.Services
{
    public class PendingSyncService : IPendingSyncService
    {
        private readonly IDbService _dbService;
        private readonly ScheduledPendingConsentSyncService _scheduledSyncService;
        private readonly IysProxy _client;
        private readonly IIysHelper _iysHelper;
        private readonly ILogger<PendingSyncService> _logger;

        public PendingSyncService(
            IDbService dbService,
            ScheduledPendingConsentSyncService scheduledSyncService,
            IysProxy client,
            IIysHelper iysHelper,
            ILogger<PendingSyncService> logger)
        {
            _dbService = dbService;
            _scheduledSyncService = scheduledSyncService;
            _client = client;
            _iysHelper = iysHelper;
            _logger = logger;
        }

        public async Task SyncAsync(IEnumerable<Consent> consents)
        {
            if (consents == null)
            {
                return;
            }

            var consentLogs = consents
                .Select(ToConsentRequestLog)
                .Where(log => log != null && !string.IsNullOrWhiteSpace(log.Recipient))
                .Cast<ConsentRequestLog>()
                .ToList();

            if (consentLogs.Count == 0)
            {
                return;
            }

            var processedRecipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var consentParamsCache = new Dictionary<string, ConsentParams>(StringComparer.OrdinalIgnoreCase);
            var pullStatusCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var markForDeferredSync = new List<long>();

            foreach (var consentLog in consentLogs)
            {
                var companyCode = !string.IsNullOrWhiteSpace(consentLog.CompanyCode)
                    ? consentLog.CompanyCode
                    : _iysHelper.GetCompanyCode(consentLog.IysCode) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(companyCode))
                {
                    if (consentLog.Id > 0)
                    {
                        markForDeferredSync.Add(consentLog.Id);
                    }

                    continue;
                }

                var recipientType = consentLog.RecipientType ?? string.Empty;
                var recipientKey = $"{companyCode}|{consentLog.Recipient}|{recipientType}";

                if (!pullStatusCache.TryGetValue(recipientKey, out var hasPulled))
                {
                    hasPulled = await _dbService.PullConsentExists(companyCode, consentLog.Recipient, consentLog.Type);
                    pullStatusCache[recipientKey] = hasPulled;
                }

                if (!hasPulled)
                {
                    if (consentLog.Id > 0)
                    {
                        markForDeferredSync.Add(consentLog.Id);
                    }

                    continue;
                }

                if (!processedRecipients.Add(recipientKey))
                {
                    continue;
                }

                try
                {
                    var queryRequest = new RecipientKey
                    {
                        Recipient = consentLog.Recipient,
                        RecipientType = consentLog.RecipientType,
                        Type = consentLog.Type
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
                            consentParams = consentLog.IysCode != 0 && consentLog.BrandCode != 0
                                ? new ConsentParams { IysCode = consentLog.IysCode, BrandCode = consentLog.BrandCode }
                                : _iysHelper.GetIysCode(companyCode);

                            if (consentParams == null)
                            {
                                _logger.LogWarning(
                                    "PendingSyncService could not resolve consent parameters for company {CompanyCode}.",
                                    companyCode);
                                continue;
                            }

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
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PendingSyncService failed while syncing recipient {Recipient}", consentLog.Recipient);
                }
            }

            if (markForDeferredSync.Count > 0)
            {
                try
                {
                    await _dbService.MarkConsentsAsNotPulled(markForDeferredSync);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PendingSyncService failed while marking consents for deferred pull");
                }
            }
        }

        public Task<ResponseBase<ScheduledJobStatistics>> RunBatchAsync(int rowCount)
        {
            return _scheduledSyncService.RunAsync(rowCount);
        }

        private static ConsentRequestLog? ToConsentRequestLog(Consent consent)
        {
            if (consent is ConsentRequestLog requestLog)
            {
                return requestLog;
            }

            if (string.IsNullOrWhiteSpace(consent.CompanyCode))
            {
                return null;
            }

            return new ConsentRequestLog
            {
                CompanyCode = consent.CompanyCode,
                Recipient = consent.Recipient,
                RecipientType = consent.RecipientType,
                Type = consent.Type,
                Status = consent.Status,
                Source = consent.Source,
                ConsentDate = consent.ConsentDate,
                Id = consent.Id
            };
        }
    }
}
