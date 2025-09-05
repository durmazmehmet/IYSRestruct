using IYSIntegration.Application.Services.Models.Base;

namespace IYSIntegration.Application.Services.Interface;

public interface IIysHelper
{
    ConsentParams GetIysCode(string companyCode);
    string? GetCompanyCode(int code);
    bool IsOlderThanBusinessDays(DateTime consentDate, int maxBusinessDays);
}