using IYS.Application.Services;
using IYS.Application.Services.Interface;
using IYS.Application.Services.Models.Response.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IYS.Proxy.API.Controllers;

[ApiController]
[Route("api/[controller]/{companyCode}")]
public class TokenController : ControllerBase
{
    private readonly IIysHelper _iysHelper;
    private readonly ICacheService _cacheService;

    public TokenController(IIysHelper iysHelper, ICacheService cacheService)
    {
        _iysHelper = iysHelper ?? throw new ArgumentNullException(nameof(iysHelper));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    [HttpGet("getTokenInfo")]
    public async Task<ActionResult<Token>> GetTokenInfo([FromRoute] string companyCode)
    {
        var consentParams = _iysHelper.GetIysCode(companyCode);
        Token? token = await _cacheService.GetCachedHashDataAsync<Token>("IYS_Token", consentParams.IysCode.ToString());

        if (token is null)
        {
            return NotFound();
        }

        return Ok(token);
    }
}
