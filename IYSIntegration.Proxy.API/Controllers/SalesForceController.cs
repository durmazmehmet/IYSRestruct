using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request;
using IYSIntegration.Application.Services.Models.Response.Consent;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;

namespace IYSIntegration.Proxy.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesForceController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ISfIdentityService _identityManager;
    private readonly string _baseUrl;

    public SalesForceController(IConfiguration config, ISfIdentityService identityManager)
    {
        _config = config;
        _baseUrl = _config.GetValue<string>($"Salesforce:BaseUrl");
        _identityManager = identityManager;
    }

    /// <summary>
    /// IYS'den gelen onay bilgisini Salesforce'a ekler.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("AddConsent")]
    public async Task<ActionResult<ResponseBase<SfConsentAddResponse>>> SalesfoceAddConsent([FromBody] SfConsentAddRequest request)
    {
        var response = new ResponseBase<SfConsentAddResponse>();

        try
        {
            var token = await _identityManager.GetToken(true);

            var client = new RestClient(new RestClientOptions(_baseUrl + "/apexrest/iys")
            {
                Authenticator = new JwtAuthenticator(token.AccessToken)
            });


            var httpRequest = new RestRequest();

            httpRequest.AddParameter("application/json", JsonConvert.SerializeObject(request,
                                         Formatting.None,
                                         new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }),
                                         ParameterType.RequestBody);

            var httpResponse = await client.PostAsync(httpRequest);

            if (httpResponse.IsSuccessful)
            {
                var result = JsonConvert.DeserializeObject<SfConsentAddResponse>(httpResponse.Content);
                response.Data = result;
                response.Status = ServiceResponseStatuses.Success;
            }
            else
            {
                var errorResponse = JsonConvert.DeserializeObject<List<SfConsentAddErrorResponse>>(httpResponse.Content);
                response.Error("Hata", $"{errorResponse.FirstOrDefault().errorCode}-{errorResponse.FirstOrDefault().message}");
            }
        }
        catch (Exception ex)
        {
            response.Error("Hata", $"{ex.Message}");
        }

        return StatusCode(response.HttpStatusCode == 0 ? 500 : response.HttpStatusCode, response);
    }
}