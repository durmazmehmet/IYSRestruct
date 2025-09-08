using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request;
using Microsoft.AspNetCore.Mvc;

namespace IYSIntegration.Proxy.API.Controllers;

[ApiController]
[Route("api/[controller]/{companyCode}")]
public class InfoController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IIysRestClientService _clientHelper;
    private readonly IIysHelper _iysHelper;
    private readonly string _baseUrl;

    public InfoController(IConfiguration config, IIysRestClientService clientHelper, IIysHelper iysHelper)
    {
        _config = config;
        _clientHelper = clientHelper;
        _baseUrl = _config.GetValue<string>("BaseUrl");
        _iysHelper = iysHelper;
    }

    [HttpGet("getCities")]
    public async Task<ActionResult<ResponseBase<List<City>>>> GetCities([FromRoute] string companyCode)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        var iysRequest = new IysRequest<DummyRequest>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/info/cities",
            Action = "Get Cities"
        };

        var result = await _clientHelper.Execute<List<City>, DummyRequest>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
    }

    [HttpGet("getCity")]
    public async Task<ActionResult<ResponseBase<City>>> GetCity([FromRoute] string companyCode, [FromQuery] string code)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        var iysRequest = new IysRequest<DummyRequest>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/info/cities/{code}",
            Action = "Get City"
        };

        var result = await _clientHelper.Execute<City, DummyRequest>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
    }

    [HttpGet("getTown")]
    public async Task<ActionResult<ResponseBase<Town>>> GetTown([FromRoute] string companyCode, [FromQuery] string townCode)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        var iysRequest = new IysRequest<DummyRequest>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/info/town/{townCode}",
            Action = "Get Town"
        };

        var result = await _clientHelper.Execute<Town, DummyRequest>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
    }

    [HttpGet("getTowns")]
    public async Task<ActionResult<ResponseBase<List<Town>>>> GetTowns([FromRoute] string companyCode)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);

        var iysRequest = new IysRequest<DummyRequest>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/info/town",
            Action = "Get Towns"
        };

        var result = await _clientHelper.Execute<List<Town>, DummyRequest>(iysRequest);

        return StatusCode(result.HttpStatusCode == 0 ? 500 : result.HttpStatusCode, result);
    }
}
