using IYS.Application.Services.Models.Base;
using IYS.Application.Services.Models.Response.Consent;

namespace IYS.Application.Services.Interface;

public interface IIysHelper
{
    ConsentParams GetIysCode(string companyCode);

    string? GetCompanyCode(int code);

    List<string> GetAllCompanyCodes();

    string? ResolveCompanyCode(string? companyCode, int iysCode);

    string BuildAddConsentErrorMessage(ResponseBase<AddConsentResult> addResponse);

    bool IsForceSendEnabled();
}
