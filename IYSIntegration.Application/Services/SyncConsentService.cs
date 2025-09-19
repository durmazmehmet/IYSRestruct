using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Response.Consent;
using Microsoft.Extensions.Logging;

namespace IYSIntegration.Application.Services
{
    public class SyncConsentService : ISyncConsentService
    {
        private readonly IDbService _dbService;
        private readonly IysProxy _client;
        private readonly ILogger<SyncConsentService> _logger;

        public SyncConsentService(IDbService dbService, IysProxy client, ILogger<SyncConsentService> logger)
        {
            _dbService = dbService;
            _client = client;
            _logger = logger;
        }

        public async Task<Consent?> SyncAsync(Consent consent, CancellationToken cancellationToken = default)
        {
            if (consent == null)
            {
                return null;
            }

            var syncedConsents = await SyncAsync(new[] { consent }, cancellationToken);
            return syncedConsents.FirstOrDefault();
        }

        public async Task<IReadOnlyCollection<Consent>> SyncAsync(IEnumerable<Consent> consents, CancellationToken cancellationToken = default)
        {
            if (consents == null)
            {
                return Array.Empty<Consent>();
            }

            var preparedConsents = consents
                .Where(consent => consent != null)
                .Select(consent => new ConsentGroupItem(
                    consent,
                    consent.CompanyCode?.Trim() ?? string.Empty,
                    consent.Recipient?.Trim() ?? string.Empty,
                    consent.RecipientType?.Trim() ?? string.Empty,
                    consent.Type?.Trim() ?? string.Empty))
                .Where(item => !string.IsNullOrWhiteSpace(item.CompanyCode)
                               && !string.IsNullOrWhiteSpace(item.Recipient)
                               && !string.IsNullOrWhiteSpace(item.RecipientType)
                               && !string.IsNullOrWhiteSpace(item.Type))
                .ToList();

            if (preparedConsents.Count == 0)
            {
                return Array.Empty<Consent>();
            }

            var approvedConsents = new List<Consent>();

            foreach (var group in preparedConsents.GroupBy(
                         item => (item.CompanyCode, item.RecipientType, item.Type),
                         ConsentGroupComparer.Instance))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var recipients = group
                    .Select(item => item.Recipient)
                    .Where(recipient => !string.IsNullOrWhiteSpace(recipient))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (recipients.Count == 0)
                {
                    continue;
                }

                try
                {
                    var request = new RecipientKeyWithList
                    {
                        RecipientType = group.Key.RecipientType,
                        Type = group.Key.Type,
                        Recipients = recipients
                    };

                    var response = await _client.PostJsonAsync<RecipientKeyWithList, MultipleQueryConsentResult>(
                        $"consents/{group.Key.CompanyCode}/queryMultipleConsent",
                        request,
                        cancellationToken);

                    if (!response.IsSuccessful())
                    {
                        _logger.LogWarning(
                            "SyncConsentService query failed for company {CompanyCode}, recipientType {RecipientType}, type {Type}.",
                            group.Key.CompanyCode,
                            group.Key.RecipientType,
                            group.Key.Type);
                        continue;
                    }

                    var approvedRecipients = response.Data?.List?.Count > 0
                        ? response.Data.List
                            .Where(recipient => !string.IsNullOrWhiteSpace(recipient))
                            .Select(recipient => recipient.Trim())
                            .ToHashSet(StringComparer.OrdinalIgnoreCase)
                        : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    var rejectedRecipients = recipients
                        .Where(recipient => approvedRecipients.Count == 0 || !approvedRecipients.Contains(recipient))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (rejectedRecipients.Count > 0)
                    {
                        await _dbService.UpdatePullConsentStatuses(
                            group.Key.CompanyCode,
                            group.Key.RecipientType,
                            group.Key.Type,
                            rejectedRecipients,
                            "RET");
                    }

                    if (approvedRecipients.Count > 0)
                    {
                        await _dbService.UpdatePullConsentStatuses(
                            group.Key.CompanyCode,
                            group.Key.RecipientType,
                            group.Key.Type,
                            approvedRecipients,
                            "ONAY");

                        approvedConsents.AddRange(group
                            .Where(item => approvedRecipients.Contains(item.Recipient))
                            .Select(item => item.Consent));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "SyncConsentService failed for company {CompanyCode}, recipientType {RecipientType}, type {Type}.",
                        group.Key.CompanyCode,
                        group.Key.RecipientType,
                        group.Key.Type);
                }
            }

            return approvedConsents;
        }

        private sealed record ConsentGroupItem(
            Consent Consent,
            string CompanyCode,
            string Recipient,
            string RecipientType,
            string Type);

        private sealed class ConsentGroupComparer : IEqualityComparer<(string CompanyCode, string RecipientType, string Type)>
        {
            public static readonly ConsentGroupComparer Instance = new();

            public bool Equals((string CompanyCode, string RecipientType, string Type) x, (string CompanyCode, string RecipientType, string Type) y)
            {
                return string.Equals(x.CompanyCode, y.CompanyCode, StringComparison.OrdinalIgnoreCase)
                       && string.Equals(x.RecipientType, y.RecipientType, StringComparison.OrdinalIgnoreCase)
                       && string.Equals(x.Type, y.Type, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode((string CompanyCode, string RecipientType, string Type) obj)
            {
                var hash = new HashCode();
                hash.Add(obj.CompanyCode, StringComparer.OrdinalIgnoreCase);
                hash.Add(obj.RecipientType, StringComparer.OrdinalIgnoreCase);
                hash.Add(obj.Type, StringComparer.OrdinalIgnoreCase);
                return hash.ToHashCode();
            }
        }
    }
}
