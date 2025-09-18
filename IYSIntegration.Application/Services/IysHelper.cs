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

    public IysHelper(
        IConfiguration config,
        IDbService dbService,
        IServiceProvider serviceProvider)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public bool IsForceSendEnabled() => _config.GetValue("ForceSend", false);

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
