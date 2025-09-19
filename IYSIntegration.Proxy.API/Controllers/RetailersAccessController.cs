using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request;
using IYSIntegration.Application.Services.Models.Response.RetailerAccess;
using Microsoft.AspNetCore.Mvc;

namespace IYSIntegration.Proxy.API.Controllers;

[ApiController]
[Route("api/[controller]/{companyCode}")]
public class RetailersAccessController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IIysRestClientService _clientHelper;
    private readonly IIysHelper _iysHelper;
    private readonly string _baseUrl;

    public RetailersAccessController(IConfiguration config, IIysRestClientService clientHelper, IIysHelper iysHelper)
    {
        _config = config;
        _clientHelper = clientHelper;
        _baseUrl = _config.GetValue<string>("BaseUrl");
        _iysHelper = iysHelper;
    }

    /// <summary>
    /// {_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents/retailers/access
    /// </summary>
    /// <param name="companyCode"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("addRetailerAccess")]
    public async Task<ActionResult<ResponseBase<AddRetailerAccessResult>>> AddRetailerAccess(
        [FromRoute] string companyCode,
        [FromBody] RetailerRecipientAccess request)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        var iysRequest = new IysRequest<RetailerRecipientAccess>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents/retailers/access",
            Body = request,
            Action = "Add Retailer Access"
        };

        var result = await _clientHelper.Execute<AddRetailerAccessResult, RetailerRecipientAccess>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
    }

    /// <summary>
    /// {_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents/retailers/access/remove/all
    /// </summary>
    /// <param name="companyCode"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("deleteAllRetailersAccess")]
    public async Task<ActionResult<ResponseBase<DeleteAllRetailersAccessResult>>> DeleteAllRetailersAccess(
        [FromRoute] string companyCode,
        [FromBody] RetailerRecipientAccess request
        )
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        var iysRequest = new IysRequest<RetailerRecipientAccess>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents/retailers/access/remove/all",
            Body = request,
            Action = "Delete All Retailers Access"
        };

        var result = await _clientHelper.Execute<DeleteAllRetailersAccessResult, RetailerRecipientAccess>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
    }

    /// <summary>
    /// {_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents/retailers/access
    /// </summary>
    /// <param name="companyCode"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("updateRetailerAccess")]
    public async Task<ActionResult<ResponseBase<UpdateRetailerAccessResult>>> UpdateRetailerAccess(
        [FromRoute] string companyCode,
        [FromBody] RetailerRecipientAccess request
        )
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        var iysRequest = new IysRequest<RetailerRecipientAccess>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consents/retailers/access",
            Body = request,
            Action = "Update Retailer Access"
        };

        var result = await _clientHelper.Execute<UpdateRetailerAccessResult, RetailerRecipientAccess>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
    }

    /// <summary>
    /// {_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/consents/retailers/access/remove
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("deleteRetailerAccess")]
    public async Task<ActionResult<ResponseBase<DeleteRetailerAccessResult>>> DeleteRetailerAccess(DeleteRetailerAccessRequest request)
    {
        var iysRequest = new IysRequest<RetailerRecipientAccess>
        {
            IysCode = request.IysCode,
            Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/consents/retailers/access/remove",
            Body = request.RetailerRecipientAccess,
            Action = "Delete Retailer Access"
        };

        var result = await _clientHelper.Execute<DeleteRetailerAccessResult, RetailerRecipientAccess>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
    }

    /// <summary>
    /// {_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consent/retailers/access/list
    /// </summary>
    /// <param name="companyCode"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("queryRetailerAccess")]
    public async Task<ActionResult<ResponseBase<QueryRetailerAccessResult>>> QueryRetailerAccess(
        [FromRoute] string companyCode,
        [FromBody] RecipientKey request
        )
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        var iysRequest = new IysRequest<RecipientKey>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/consent/retailers/access/list",
            Body = request,
            Action = "Query Retailer Access"
        };

        var result = await _clientHelper.Execute<QueryRetailerAccessResult, RecipientKey>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
    }
}
