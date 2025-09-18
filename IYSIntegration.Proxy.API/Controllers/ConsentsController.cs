using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request;
using IYSIntegration.Application.Services.Models.Request.Consent;
using IYSIntegration.Application.Services.Models.Response.Consent;
using Microsoft.AspNetCore.Mvc;

namespace IYSIntegration.Proxy.API.Controllers;

[ApiController]
[Route("api/[controller]/{companyCode}")]
public class ConsentsController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IIysRestClientService _clientHelper;
    private readonly IIysHelper _iysHelper;
    private readonly string _baseUrl;

    public ConsentsController(IConfiguration config, IIysRestClientService clientHelper, IIysHelper iysHelper)
    {
        _config = config;
        _clientHelper = clientHelper;
        _baseUrl = _config.GetValue<string>("BaseUrl");
        _iysHelper = iysHelper;
    }

    /// <summary>
    /// "{_baseUrl}/sps/{IysCode}/brands/{BrandCode}/consents"
    /// </summary>
    /// <param name="companyCode"></param>
    /// <param name="consent"></param>
    /// <returns></returns>
    [HttpPost("addConsent")]
    public async Task<ActionResult<ResponseBase<AddConsentResult>>> AddConsent(
        [FromRoute] string companyCode,
        [FromBody] Consent consent)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        var iysRequest = new IysRequest<Consent>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents",
            Body = consent,
            Action = "Add Consent",
            Method = RestSharp.Method.Post
        };

        var result = await _clientHelper.Execute<AddConsentResult, Consent>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
    }

    /// <summary>
    /// "{_baseUrl}/sps/{IysCode}/brands/{BrandCode}/consents/status"
    /// </summary>
    /// <param name="companyCode"></param>
    /// <param name="recipientKey"></param>
    /// <returns></returns>
    [HttpPost("queryConsent")]
    public async Task<ActionResult<ResponseBase<QueryConsentResult>>> QueryConsent(
        [FromRoute] string companyCode,
        [FromBody] RecipientKey recipientKey)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        var iysRequest = new IysRequest<RecipientKey>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents/status",
            Body = recipientKey,
            Action = "Query Consent",
            Method = RestSharp.Method.Post
        };

        var result = await _clientHelper.Execute<QueryConsentResult, RecipientKey>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
    }

    /// <summary>
    /// "{_baseUrl}/sps/{IysCode}/brands/{BrandCode}/consents/request"
    /// </summary>
    /// <param name="companyCode"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("addMultipleConsent")]
    public async Task<ActionResult<ResponseBase<MultipleConsentResult>>> AddMultipleConsent(
        [FromRoute] string companyCode,
        [FromBody] MultipleConsentRequest request)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        var iysRequest = new IysRequest<List<Consent>>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents/request",
            Body = request.Consents,
            Action = "Add Multiple Consent",
            BatchId = request.BatchId
        };

        var result = await _clientHelper.Execute<MultipleConsentResult, List<Consent>>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
    }

    /// <summary>
    /// "{_baseUrl}/sps/{IysCode}/brands/{BrandCode}/consents/request/{requestId}"
    /// </summary>
    /// <param name="companyCode"></param>
    /// <param name="requestId"></param>
    /// <param name="batchId"></param>
    /// <returns></returns>
    [HttpGet("queryMultipleConsent")]
    public async Task<ActionResult<ResponseBase<List<QueryMultipleConsentResult>>>> QueryMultipleConsent(
        [FromRoute] string companyCode,
        [FromQuery] string requestId, int? batchId = null)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        var iysRequest = new IysRequest<DummyRequest>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents/request/{Uri.EscapeDataString(requestId)}",
            Action = "Query Multiple Consent",
            BatchId = batchId
        };

        var result = await _clientHelper.Execute<List<QueryMultipleConsentResult>, DummyRequest>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
    }

    [HttpPost("searchRequestDetailsV2")]
    public async Task<ActionResult<ResponseBase<List<QueryMultipleConsentResultV2>>>> SearchRequestDetailsV2(
        [FromRoute] string companyCode,
        [FromBody] QueryMultipleConsentRequestV2 request)
    {
        if (request is null)
        {
            return BadRequest("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RequestId))
        {
            return BadRequest("requestId is required.");
        }

        var consentParams = _iysHelper.GetIysCode(companyCode);

        var iysRequest = new IysRequest<DummyRequest>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/v2/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents/request/{Uri.EscapeDataString(request.RequestId)}",
            Action = "Search Request Details V2",
            BatchId = request.BatchId
        };

        var result = await _clientHelper.Execute<List<QueryMultipleConsentResultV2>, DummyRequest>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
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
    public async Task<ActionResult<ResponseBase<PullConsentResult>>> PullConsent(
        [FromRoute] string companyCode,
        [FromQuery] string? after = null, int limit = 0, string source = "IYS")
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        var queryParams = new List<string> { $"source={Uri.EscapeDataString(source)}" };

        if (limit > 0) queryParams.Add($"limit={limit}");

        if (!string.IsNullOrWhiteSpace(after)) queryParams.Add($"after={Uri.EscapeDataString(after)}");

        var iysRequest = new IysRequest<DummyRequest>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents/changes?" + string.Join("&", queryParams),
            Action = "Pull Consent"
        };

        var result = await _clientHelper.Execute<PullConsentResult, DummyRequest>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
    }
}
