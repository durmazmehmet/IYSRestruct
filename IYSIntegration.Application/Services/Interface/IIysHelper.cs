using IYSIntegration.Application.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IYSIntegration.Application.Services.Interface;

public interface IIysHelper
{
    ConsentParams GetIysCode(string companyCode);
    string? GetCompanyCode(int code);
    bool IsOlderThanBusinessDays(DateTime consentDate, int maxBusinessDays);
}