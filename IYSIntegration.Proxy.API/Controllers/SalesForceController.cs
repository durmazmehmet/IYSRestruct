using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request.Consent;
using IYSIntegration.Application.Services.Models.Response.Consent;
using Microsoft.AspNetCore.Mvc;

namespace IYSIntegration.Proxy.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesForceController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ISimpleRestClient _clientHelper;
    private readonly IIysHelper _iysHelper;
    private readonly ISfIdentityService _identityManager;
    private readonly string _baseUrl;

    public SalesForceController(IConfiguration config, ISimpleRestClient clientHelper, IIysHelper iysHelper, ISfIdentityService identityManager)
    {
        _config = config;
        _clientHelper = clientHelper;
        _baseUrl = _config.GetValue<string>($"SalesforceBaseUrl");
        _iysHelper = iysHelper;
        _identityManager = identityManager;
    }


    [HttpPost("AddConsent")]
    public async Task<ResponseBase<SfConsentAddResponse>> SalesfoceAddConsent([FromBody] SfConsentAddRequest request)
    {
        var token = await _identityManager.GetToken(true);

        _clientHelper.AddAuthorization("Bearer", token.AccessToken);

        // new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }),????

        return await _clientHelper.PostJsonAsync<SfConsentAddRequest, SfConsentAddResponse>($"{_baseUrl}/apexrest/iys", request);
    }
}