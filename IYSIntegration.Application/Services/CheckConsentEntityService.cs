using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using Microsoft.Extensions.Logging;

namespace IYSIntegration.Application.Services
{
    public class CheckConsentEntityService
    {
        private readonly IDbService _dbService;
        private readonly ILogger<CheckConsentEntityService> _logger;

        public CheckConsentEntityService(IDbService dbService, ILogger<CheckConsentEntityService> logger)
        {
            _dbService = dbService;
            _logger = logger;
        }

        public async Task<bool> HasExistingConsentAsync(Consent consent)
        {
            if (consent == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(consent.CompanyCode) || string.IsNullOrWhiteSpace(consent.Recipient))
            {
                _logger.LogWarning("Consent is missing required fields: CompanyCode='{CompanyCode}', Recipient='{Recipient}'", consent.CompanyCode, consent.Recipient);
                return false;
            }

            try
            {
                var type = string.IsNullOrWhiteSpace(consent.Type) ? null : consent.Type;

                if (await _dbService.PullConsentExists(consent.CompanyCode!, consent.Recipient!, type))
                {
                    return true;
                }

                return await _dbService.SuccessfulConsentRequestExists(consent.CompanyCode!, consent.Recipient!, type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check consent existence for CompanyCode='{CompanyCode}', Recipient='{Recipient}'", consent.CompanyCode, consent.Recipient);
                return false;
            }
        }

        public async Task<ConsentExistenceResult> HasExistingConsentAsync(IEnumerable<Consent> consents)
        {
            var result = new ConsentExistenceResult();

            if (consents == null)
            {
                return result;
            }

            var validConsents = consents
                .Where(c => c != null)
                .ToList();

            var missingCompanyConsents = validConsents
                .Where(c => string.IsNullOrWhiteSpace(c.CompanyCode))
                .ToList();

            foreach (var consent in missingCompanyConsents)
            {
                _logger.LogWarning("Consent is missing required company code.");
                result.NonConsents.Add(consent);
            }

            var groupedConsents = validConsents
                .Where(c => !string.IsNullOrWhiteSpace(c.CompanyCode))
                .GroupBy(c => new
                {
                    CompanyCode = c!.CompanyCode!.Trim(),
                    Type = string.IsNullOrWhiteSpace(c?.Type) ? null : c!.Type!.Trim()
                });

            foreach (var group in groupedConsents)
            {
                var companyCode = group.Key.CompanyCode;
                var type = group.Key.Type;
                var recipients = group
                    .Where(c => !string.IsNullOrWhiteSpace(c.Recipient))
                    .Select(c => c.Recipient!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (!recipients.Any())
                {
                    foreach (var consent in group)
                    {
                        _logger.LogWarning("Consent is missing required recipient for CompanyCode='{CompanyCode}'", companyCode);
                        result.NonConsents.Add(consent);
                    }

                    continue;
                }

                List<string> existingRecipients;

                try
                {
                    existingRecipients = await _dbService.GetExistingConsentRecipients(companyCode, type, recipients);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch existing consents for CompanyCode='{CompanyCode}', Type='{Type}'", companyCode, type);

                    foreach (var consent in group)
                    {
                        result.NonConsents.Add(consent);
                    }

                    continue;
                }

                var existingRecipientSet = new HashSet<string>(
                    (existingRecipients ?? new List<string>())
                        .Where(r => !string.IsNullOrWhiteSpace(r))
                        .Select(r => r.Trim()),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var consent in group)
                {
                    if (string.IsNullOrWhiteSpace(consent.Recipient))
                    {
                        _logger.LogWarning("Consent is missing required recipient for CompanyCode='{CompanyCode}'", companyCode);
                        result.NonConsents.Add(consent);
                        continue;
                    }

                    var recipient = consent.Recipient!.Trim();

                    if (existingRecipientSet.Contains(recipient))
                    {
                        result.ExistConsents.Add(consent);
                    }
                    else
                    {
                        result.NonConsents.Add(consent);
                    }
                }
            }

            return result;
        }
    }
}
