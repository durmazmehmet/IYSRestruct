using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request.Consent;
using IYSIntegration.Application.Services.Models.Response.Consent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IYSIntegration.Application.Services;

public sealed class IysHelper : IIysHelper
{
    private readonly IConfiguration _config;
    private readonly IDbService _dbService;
    private readonly IServiceProvider _serviceProvider;
    private IDuplicateCleanerService? _duplicateCleanerService;
    private IPendingSyncService? _pendingSyncService;

    public IysHelper(
        IConfiguration config,
        IDbService dbService,
        IServiceProvider serviceProvider)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public List<string> GetAllCompanyCodes() => _config.GetSection("CompanyCodes").Get<List<string>>() ?? [];

    public ConsentParams GetIysCode(string companyCode)
    {
        if (string.IsNullOrWhiteSpace(companyCode))
            throw new ArgumentException("Şirket kodu boş olamaz.", nameof(companyCode));

        if (TryGet(companyCode, out var result))
            return result;

        throw new InvalidOperationException($"'{companyCode}' için IysCode/BrandCode bulunamadı.");
    }

    public bool TryGet(string companyCode, out ConsentParams result)
    {
        var iys = _config.GetValue<int?>($"{companyCode}:IysCode");
        var brand = _config.GetValue<int?>($"{companyCode}:BrandCode");

        if (iys is null || brand is null)
        {
            result = default!;
            return false;
        }

        result = new ConsentParams
        {
            IysCode = iys.Value,
            BrandCode = brand.Value
        };
        return true;
    }

    public string? GetCompanyCode(int code)
    {
        foreach (var section in _config.GetChildren())
        {
            var iys = _config.GetValue<int?>($"{section.Key}:IysCode");
            var brand = _config.GetValue<int?>($"{section.Key}:BrandCode");

            if (iys == code || brand == code)
                return section.Key;
        }

        return null;
    }

    public bool IsOlderThanBusinessDays(DateTime consentDate, int maxBusinessDays)
    {
        var date = consentDate.Date;
        var today = DateTime.Now.Date;
        int businessDays = 0;

        while (date < today)
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                businessDays++;
            }

            if (businessDays >= maxBusinessDays)
            {
                return true;
            }

            date = date.AddDays(1);
        }

        return false;
    }

    public async Task<(bool IsValid, ResponseBase<AddConsentResult> Response)> ValidateConsentRequestAsync(
        AddConsentRequest request)
    {
        var response = new ResponseBase<AddConsentResult>();

        request.CompanyCode = ResolveCompanyCode(request.CompanyCode, request.CompanyName, request.IysCode);

        if (request.ForceSend)
        {
            return (true, response);
        }

        if (request.Consent == null)
        {
            response.Error("Hata", "Consent bilgisi zorunludur");
            return (false, response);
        }

        if (string.IsNullOrWhiteSpace(request.Consent.ConsentDate))
        {
            response.Error("Hata", "ConsentDate alanı zorunludur");
            return (false, response);
        }

        if (!DateTime.TryParse(request.Consent.ConsentDate, out var parsedDate))
        {
            response.Error("Hata", "ConsentDate alanı geçersiz");
            return (false, response);
        }

        if (!await _dbService.CheckConsentRequest(request))
        {
            response.Error("Hata", "İlk defa giden rıza red gönderilemez");
            return (false, response);
        }

        var lastConsentDate = await _dbService.GetLastConsentDate(request.CompanyCode!, request.Consent.Recipient);

        if (lastConsentDate.HasValue && parsedDate < lastConsentDate.Value)
        {
            response.Error("Validation", "Sistemdeki izinden eski tarihli rıza gönderilemez");
            return (false, response);
        }

        if (IsOlderThanBusinessDays(parsedDate, 3))
        {
            response.Error("Hata", "3 iş gününden eski consent gönderilemez");
            return (false, response);
        }

        return (true, response);
    }

    public async Task<List<ConsentProcessingResult>> ValidateMultipleConsentsAsync(
        MultipleConsentRequest request)
    {
        request.CompanyCode = ResolveCompanyCode(request.CompanyCode, request.CompanyName, request.IysCode);

        var results = new List<ConsentProcessingResult>();

        for (var i = 0; i < request.Consents.Count; i++)
        {
            var consent = request.Consents[i];
            var addConsentRequest = new AddConsentRequest
            {
                CompanyCode = request.CompanyCode,
                CompanyName = request.CompanyName,
                IysCode = request.IysCode,
                BrandCode = request.BrandCode,
                SalesforceId = consent.SalesforceId,
                ForceSend = request.ForceSend,
                Consent = new Consent
                {
                    ConsentDate = consent.ConsentDate,
                    Source = consent.Source,
                    Recipient = consent.Recipient,
                    RecipientType = consent.RecipientType,
                    Status = consent.Status,
                    Type = consent.Type,
                    RetailerCode = consent.RetailerCode,
                    RetailerAccess = consent.RetailerAccess,
                    SalesforceId = consent.SalesforceId
                }
            };

            if (request.ForceSend)
            {
                results.Add(new ConsentProcessingResult
                {
                    Index = i + 1,
                    Request = addConsentRequest,
                    ErrorResponse = null
                });
                continue;
            }

            var (isValid, validationResponse) = await ValidateConsentRequestAsync(addConsentRequest);

            results.Add(new ConsentProcessingResult
            {
                Index = i + 1,
                Request = addConsentRequest,
                ErrorResponse = isValid ? null : validationResponse
            });
        }

        return results;
    }

    public void AppendValidationMessages(
        ResponseBase<MultipleConsentResult> response,
        ConsentProcessingResult consentResult)
    {
        if (consentResult.ErrorResponse?.Messages == null)
        {
            return;
        }

        foreach (var message in consentResult.ErrorResponse.Messages)
        {
            response.AddMessage($"{message.Key}_{consentResult.Index}", message.Value);
        }
    }

    public string BuildAddConsentErrorMessage(ResponseBase<AddConsentResult> addResponse)
    {
        if (addResponse.Messages is { Count: > 0 })
        {
            return string.Join(" | ", addResponse.Messages.Select(kv => $"{kv.Key}: {kv.Value}"));
        }

        if (!string.IsNullOrWhiteSpace(addResponse.OriginalError?.Message))
        {
            return addResponse.OriginalError.Message;
        }

        return $"HTTP {addResponse.HttpStatusCode}";
    }

    public async Task LogConsentAsync(
        AddConsentRequest request,
        ResponseBase<AddConsentResult> response,
        bool runPendingSync = true)
    {
        if (request.ForceSend)
        {
            return;
        }

        var id = await LogConsentRequestAsync(request, runPendingSync);
        response.Id = id;
        await _dbService.UpdateConsentResponseFromCommon(response);
        response.OriginalError = null;
    }

    public async Task<int> LogConsentRequestAsync(
        AddConsentRequest request,
        bool runPendingSync = true)
    {
        if (request.ForceSend)
        {
            return 0;
        }

        NormalizeConsentRequest(request);

        var id = await _dbService.InsertConsentRequest(request);

        if (id > 0 && request.Consent != null)
        {
            var insertedConsent = new ConsentRequestLog
            {
                Id = id,
                CompanyCode = request.CompanyCode,
                IysCode = request.IysCode,
                BrandCode = request.BrandCode,
                Recipient = request.Consent.Recipient,
                RecipientType = request.Consent.RecipientType,
                Type = request.Consent.Type,
                Status = request.Consent.Status,
                Source = request.Consent.Source,
                ConsentDate = request.Consent.ConsentDate
            };

            var insertedConsents = new List<Consent> { insertedConsent };

            var duplicateCleanerService = GetDuplicateCleanerService();
            if (duplicateCleanerService != null)
            {
                await duplicateCleanerService.CleanAsync(insertedConsents);
            }

            if (runPendingSync)
            {
                QueuePendingSync(insertedConsents);
            }
        }

        return id;
    }

    private void NormalizeConsentRequest(AddConsentRequest request)
    {
        if (request == null)
        {
            return;
        }

        request.CompanyCode = ResolveCompanyCode(request.CompanyCode, request.CompanyName, request.IysCode);

        if (request.IysCode == 0 && !string.IsNullOrWhiteSpace(request.CompanyCode))
        {
            var consentParams = GetIysCode(request.CompanyCode);
            request.IysCode = consentParams.IysCode;
            request.BrandCode = consentParams.BrandCode;
        }
    }

    private IDuplicateCleanerService? GetDuplicateCleanerService()
    {
        if (_duplicateCleanerService != null)
        {
            return _duplicateCleanerService;
        }

        _duplicateCleanerService = _serviceProvider.GetService<IDuplicateCleanerService>();
        return _duplicateCleanerService;
    }

    private IPendingSyncService? GetPendingSyncService()
    {
        if (_pendingSyncService != null)
        {
            return _pendingSyncService;
        }

        _pendingSyncService = _serviceProvider.GetService<IPendingSyncService>();
        return _pendingSyncService;
    }

    private void QueuePendingSync(IEnumerable<Consent> consents)
    {
        var pendingSyncService = GetPendingSyncService();
        if (pendingSyncService == null)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await pendingSyncService.SyncAsync(consents).ConfigureAwait(false);
            }
            catch
            {
                // arka planda tetiklenen senkronizasyon hataları isteği engellememelidir.
            }
        });
    }

    public string? ResolveCompanyCode(string? companyCode, string? companyName, int iysCode)
    {
        if (!string.IsNullOrWhiteSpace(companyCode))
        {
            return companyCode.Trim();
        }

        if (!string.IsNullOrWhiteSpace(companyName))
        {
            return companyName.Trim();
        }

        return iysCode != 0
            ? GetCompanyCode(iysCode)
            : null;
    }
}
