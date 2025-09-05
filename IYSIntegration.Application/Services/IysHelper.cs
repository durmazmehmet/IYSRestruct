using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using Microsoft.Extensions.Configuration;

namespace IYSIntegration.Application.Services;

public sealed class IysHelper : IIysHelper
{
    private readonly IConfiguration _config;

    public IysHelper(IConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
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
}
