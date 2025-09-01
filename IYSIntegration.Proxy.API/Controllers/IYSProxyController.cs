using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Base;
using IYSIntegration.Application.Request;
using IYSIntegration.Application.Response.Consent;
using Microsoft.AspNetCore.Mvc;

namespace IYSIntegration.Proxy.API.Controllers;

[ApiController]
[Route("api/[controller]/{companyCode}")]
public class IYSProxyController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IRestClientService _clientHelper;
    private readonly string _baseUrl;

    public IYSProxyController(IConfiguration config, IRestClientService clientHelper)
    {
        _config = config;
        _clientHelper = clientHelper;
        _baseUrl = _config.GetValue<string>("BaseUrl");
    }

    /// <summary>
    /// "{_baseUrl}/sps/{IysCode}/brands/{BrandCode}/consents"
    /// </summary>
    /// <param name="companyCode"></param>
    /// <param name="consent"></param>
    /// <returns></returns>
    [HttpPost("addConsent")]
    public async Task<IActionResult> AddConsent([FromRoute] string companyCode, [FromBody] Consent consent)
    {
        var consentParams = GetIysCode(companyCode);

        var request = new IysRequest<Consent>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents",
            Body = consent,
            Action = "Add Consent",
            Method = RestSharp.Method.Post
        };

        var result = await _clientHelper.Execute<AddConsentResult, Consent>(request);
        return Ok(result);
    }

    /// <summary>
    /// "{_baseUrl}/sps/{IysCode}/brands/{BrandCode}/consents/status"
    /// </summary>
    /// <param name="companyCode"></param>
    /// <param name="recipientKey"></param>
    /// <returns></returns>
    [HttpPost("queryConsent")]
    public async Task<IActionResult> SearchConsent([FromRoute] string companyCode, [FromBody] RecipientKey recipientKey)
    {
        var consentParams = GetIysCode(companyCode);

        var request = new IysRequest<RecipientKey>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents/status",
            Body = recipientKey,
            Action = "Query Consent",
            Method = RestSharp.Method.Post
        };

        var result = await _clientHelper.Execute<QueryConsentResult, RecipientKey>(request);
        return Ok(result);
    }

    /// <summary>
    /// "{_baseUrl}/sps/{IysCode}/brands/{BrandCode}/consents/request"
    /// </summary>
    /// <param name="companyCode"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("addMultipleConsent")]
    public async Task<IActionResult> AddMultipleConsent([FromRoute] string companyCode, [FromBody] MultipleConsentRequestDto request)
    {
        var consentParams = GetIysCode(companyCode);

        var iysRequest = new IysRequest<List<Consent>>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents/request",
            Body = request.Consents,
            Action = "Add Multiple Consent",
            BatchId = request.BatchId
        };

        var result = await _clientHelper.Execute<MultipleConsentResult, List<Consent>>(iysRequest);
        return Ok(result);
    }

    /// <summary>
    /// "{_baseUrl}/sps/{IysCode}/brands/{BrandCode}/consents/request/{requestId}"
    /// </summary>
    /// <param name="companyCode"></param>
    /// <param name="requestId"></param>
    /// <param name="batchId"></param>
    /// <returns></returns>
    [HttpGet("queryMultipleConsent")]
    public async Task<IActionResult> SearchMultipleConsent(
        [FromRoute] string companyCode,
        [FromQuery] string requestId,
        [FromQuery] int? batchId = null)
    {
        var consentParams = GetIysCode(companyCode);

        var url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents/request/{Uri.EscapeDataString(requestId)}";

        var iysRequest = new IysRequest<DummyRequest>
        {
            IysCode = consentParams.IysCode,
            Url = url,
            Action = "Query Multiple Consent",
            BatchId = batchId
        };

        var result = await _clientHelper.Execute<List<QueryMultipleConsentResult>, DummyRequest>(iysRequest);
        return Ok(result);
    }

    /// <summary>
    /// "{_baseUrl}/sps/{IysCode}/brands/{BrandCode}/consents/changes?queryParams"
    /// </summary>
    /// <param name="companyCode"></param>
    /// <param name="after"></param>
    /// <param name="limit"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    [HttpGet("pullConsent")]
    public async Task<IActionResult> PullConsent(
        [FromRoute] string companyCode,
        [FromQuery] string? after = null,
        [FromQuery] int limit = 0,
        [FromQuery] string source = "IYS")
    {
        var consentParams = GetIysCode(companyCode);

        var queryParams = new List<string> { $"source={Uri.EscapeDataString(source)}" };
        if (limit > 0) queryParams.Add($"limit={limit}");
        if (!string.IsNullOrWhiteSpace(after)) queryParams.Add($"after={Uri.EscapeDataString(after)}");

        var url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents/changes?" +
                  string.Join("&", queryParams);

        var iysRequest = new IysRequest<DummyRequest>
        {
            IysCode = consentParams.IysCode,
            Url = url,
            Action = "Pull Consent"
        };

        var result = await _clientHelper.Execute<PullConsentResult, DummyRequest>(iysRequest);
        return Ok(result);
    }

    private ConsentParams GetIysCode(string companyCode)
    {
        var iysCode = _config.GetValue<int?>($"{companyCode}:IysCode");
        var brandCode = _config.GetValue<int?>($"{companyCode}:BrandCode");

        if (iysCode == null || brandCode == null)
            throw new InvalidOperationException($"'{companyCode}' için eirşim bilgisi mevcut değil.");

        return new ConsentParams
        {
            IysCode = iysCode.Value,
            BrandCode = brandCode.Value
        };
    }
}
