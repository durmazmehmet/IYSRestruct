using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request;
using IYSIntegration.Application.Services.Models.Response.Retailer;
using Microsoft.AspNetCore.Mvc;

namespace IYSIntegration.Proxy.API.Controllers;

[ApiController]
[Route("api/[controller]/{companyCode}")]
public class RetailersController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IIysRestClientService _clientHelper;
    private readonly IIysHelper _iysHelper;
    private readonly string _baseUrl;

    public RetailersController(IConfiguration config, IIysRestClientService clientHelper, IIysHelper iysHelper)
    {
        _config = config;
        _clientHelper = clientHelper;
        _baseUrl = _config.GetValue<string>("BaseUrl");
        _iysHelper = iysHelper;
    }

    /// <summary>
    /// {sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/retailers
    /// </summary>
    /// <param name="companyCode"></param>
    /// <param name="retailer"></param>
    /// <returns></returns>
    [HttpPost("addRetailer")]
    public async Task<ResponseBase<AddRetailerResponse>> AddConsent(
        [FromRoute] string companyCode,
        [FromBody] Retailer retailer)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        return await _clientHelper.Execute<AddRetailerResponse, Retailer>(new IysRequest<Retailer>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/retailers",
            Body = retailer,
            Action = "Add Retailer"
        });

    }

    /// <summary>
    /// {_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/retailers/{retailerCode}
    /// </summary>
    /// <param name="companyCode"></param>
    /// <param name="retailerCode"></param>
    /// <returns></returns>
    [HttpGet("getRetailer")]
    public async Task<ResponseBase<GetRetailerResponse>> GetRetailer(
        [FromRoute] string companyCode,
        [FromQuery] string retailerCode
        )
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        return await _clientHelper.Execute<GetRetailerResponse, DummyRequest>(new IysRequest<DummyRequest>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/retailers/{retailerCode}",
            Action = "Get Retailer"
        });
    }

    /// <summary>
    /// {_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/retailers
    /// </summary>
    /// <param name="companyCode"></param>
    /// <returns></returns>
    [HttpGet("getAllRetailers")]
    public async Task<ResponseBase<GetAllRetailersResponse>> GetAllRetailers([FromRoute] string companyCode)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        return await _clientHelper.Execute<GetAllRetailersResponse, DummyRequest>(new IysRequest<DummyRequest>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/retailers",
            Action = "Get All Retailers"
        });
    }

    /// <summary>
    /// {_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/retailers/{retailerCode}
    /// </summary>
    /// <param name="companyCode"></param>
    /// <param name="retailerCode"></param>
    /// <returns></returns>
    [HttpGet("deleteRetailer")]
    public async Task<ResponseBase<DeleteRetailerResponse>> DeleteRetailer(
        [FromRoute] string companyCode,
        [FromQuery] string retailerCode)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        return await _clientHelper.Execute<DeleteRetailerResponse, DummyRequest>(new IysRequest<DummyRequest>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands/{consentParams.BrandCode}/retailers/{retailerCode}",
            Action = "Delete Retailer"
        });
    }


}
