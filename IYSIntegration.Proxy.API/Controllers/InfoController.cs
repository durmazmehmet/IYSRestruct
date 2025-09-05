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
    public async Task<ResponseBase<List<City>>> GetCities([FromRoute] string companyCode)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);
        return await _clientHelper.Execute<List<City>, DummyRequest>(new IysRequest<DummyRequest>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/info/cities",
            Action = "Get Cities"
        });
    }

    [HttpGet("getCity")]
    public async Task<ResponseBase<City>> GetCity([FromRoute] string companyCode, [FromQuery] string code)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);
        return await _clientHelper.Execute<City, DummyRequest>(new IysRequest<DummyRequest>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/info/cities/{code}",
            Action = "Get City"
        });
    }

    [HttpGet("getTown")]
    public async Task<ResponseBase<Town>> GetTown([FromRoute] string companyCode, [FromQuery] string townCode)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);
        return await _clientHelper.Execute<Town, DummyRequest>(new IysRequest<DummyRequest>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/info/town/{townCode}",
            Action = "Get Town"
        });
    }

    [HttpGet("getTowns")]
    public async Task<ResponseBase<List<Town>>> GetTowns([FromRoute] string companyCode)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);
        return await _clientHelper.Execute<List<Town>, DummyRequest>(new IysRequest<DummyRequest>
        {
            IysCode = consentParams.IysCode,
            Url = $"{_baseUrl}/info/town",
            Action = "Get Towns"
        });
    }
}
