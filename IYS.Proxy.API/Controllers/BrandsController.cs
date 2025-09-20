using IYS.Application.Services.Interface;
using IYS.Application.Services.Models.Base;
using IYS.Application.Services.Models.Request;
using IYS.Application.Services.Models.Response.Brand;
using Microsoft.AspNetCore.Mvc;

namespace IYS.Proxy.API.Controllers;

[ApiController]
[Route("api/[controller]/{companyCode}")]
public class BrandsController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IIysRestClientService _clientHelper;
    private readonly IIysHelper _iysHelper;
    private readonly string _baseUrl;

    public BrandsController(IConfiguration config, IIysRestClientService clientHelper, IIysHelper iysHelper)
    {
        _config = config;
        _clientHelper = clientHelper;
        _baseUrl = _config.GetValue<string>("BaseUrl");
        _iysHelper = iysHelper;
    }

    /// <summary>
    /// {_baseUrl}/sps/{consentParams.IysCode}/brands
    /// </summary>
    /// <param name="companyCode"></param>
    /// <returns></returns>
    [HttpGet("GetAll")]
    public async Task<ActionResult<ResponseBase<List<Brand>>>> GetAll(
        [FromRoute] string companyCode
        )
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        var iysRequest = new IysRequest<DummyRequest>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/sps/{consentParams.IysCode}/brands",
            Action = "Get Brands"
        };

        var result = await _clientHelper.Execute<List<Brand>, DummyRequest>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
    }
}
