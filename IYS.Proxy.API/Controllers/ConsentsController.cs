using IYS.Application.Services.Interface;
using IYS.Application.Services.Models.Base;
using IYS.Application.Services.Models.Request;
using IYS.Application.Services.Models.Response.Consent;
using Microsoft.AspNetCore.Mvc;

namespace IYS.Proxy.API.Controllers;

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
    /// Tekli izin ekleme
    /// "{_baseUrl}/v2/sps/{IysCode}/brands/{BrandCode}/consents"
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
            Url = $"{_baseUrl}/v2/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents",
            Body = consent,
            Action = "Add Consent",
            Method = RestSharp.Method.Post
        };

        var result = await _clientHelper.Execute<AddConsentResult, Consent>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
    }

    /// <summary>
    /// Tekli izin sorgulama 
    /// Bu metot, bir IP adresinden saatte en fazla 100 istek yapabilir.
    /// Çoklu versiyonu kullanmanız tavsiye edilir
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
    /// Çoklu izin sorgulama 
    /// Bu metot, aynı anda en fazla 1000 adet iletişim adresinin izin durumunun sorgulanmasını sağlar.
    /// "{_baseUrl}/sps/{IysCode}/brands/{BrandCode}/consents/status"
    /// </summary>
    /// <param name="companyCode"></param>
    /// <param name="recipientKeyWithList"></param>
    /// <returns></returns>
    [HttpPost("queryMultipleConsent")]
    public async Task<ActionResult<ResponseBase<MultipleQueryConsentResult>>> QueryMultipleConsent(
        [FromRoute] string companyCode,
        [FromBody] RecipientKeyWithList recipientKeyWithList)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        var iysRequest = new IysRequest<RecipientsRequestWrapper>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents/{recipientKeyWithList.RecipientType}/status/{recipientKeyWithList.Type}",
            Body = new RecipientsRequestWrapper { Recipients = recipientKeyWithList.Recipients },
            Action = "Query Consent",
            Method = RestSharp.Method.Post
        };

        var result = await _clientHelper.Execute<MultipleQueryConsentResult, RecipientsRequestWrapper>(iysRequest);

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


    [HttpPost("addMultipleConsent")]
    public async Task<ActionResult<ResponseBase<MultipleConsentResult>>> AddMultipleConsent(
        [FromRoute] string companyCode,
        [FromBody] List<Consent> consent)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        var iysRequest = new IysRequest<List<Consent>>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/v2/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/request",
            Body = consent,
            Action = "Add Consent",
            Method = RestSharp.Method.Post
        };

        var result = await _clientHelper.Execute<MultipleConsentResult, List<Consent>>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
    }

    /// <summary>
    /// Çoklu izin ekleme isteği sorgulama
    /// "{_baseUrl}/sps/{IysCode}/brands/{BrandCode}/consents/request/{requestId}"
    /// </summary>
    /// <param name="companyCode"></param>
    /// <param name="requestId"></param>
    /// <returns></returns>
    [HttpGet("queryMultipleConsentRequest/{requestId}")]
    public async Task<ActionResult<ResponseBase<MultipleConsentRequestStatusResult>>> QueryMultipleConsentRequest(
        [FromRoute] string companyCode,
        [FromRoute] string requestId)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        var iysRequest = new IysRequest<DummyRequest>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents/request/{requestId}",
            Action = "Query Multiple Consent Request",
            Method = RestSharp.Method.Get
        };

        var result = await _clientHelper.Execute<MultipleConsentRequestStatusResult, DummyRequest>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
    }
}
