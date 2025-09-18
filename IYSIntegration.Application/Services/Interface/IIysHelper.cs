using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request.Consent;
using IYSIntegration.Application.Services.Models.Response.Consent;

namespace IYSIntegration.Application.Services.Interface;

public interface IIysHelper
{
    ConsentParams GetIysCode(string companyCode);
    string? GetCompanyCode(int code);
    bool IsOlderThanBusinessDays(DateTime consentDate, int maxBusinessDays);
    List<string> GetAllCompanyCodes();
    Task<(bool IsValid, ResponseBase<AddConsentResult> Response)> ValidateConsentRequestAsync(
        AddConsentRequest request,
        IDbService dbService);

    Task<List<ConsentProcessingResult>> ValidateMultipleConsentsAsync(
        MultipleConsentRequest request,
        IDbService dbService);

    void AppendValidationMessages(
        ResponseBase<MultipleConsentResult> response,
        ConsentProcessingResult consentResult);

    Task LogConsentAsync(
        AddConsentRequest request,
        ResponseBase<AddConsentResult> response,
        IDbService dbService,
        IDuplicateCleanerService duplicateCleanerService,
        IPendingSyncService pendingSyncService);

    Task<int> LogConsentRequestAsync(
        AddConsentRequest request,
        IDbService dbService,
        IDuplicateCleanerService duplicateCleanerService,
        IPendingSyncService pendingSyncService);

    string? ResolveCompanyCode(string? companyCode, string? companyName, int iysCode);
}
