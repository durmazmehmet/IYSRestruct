using IYSIntegration.Application.Services;
using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Error;
using IYSIntegration.Application.Services.Models.Request.Consent;
using IYSIntegration.Application.Services.Models.Response.Consent;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace IYSIntegration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommonController : ControllerBase
    {
        private readonly IDbService _dbService;
        private readonly IysProxy _client;
        private readonly IIysHelper _iysHelper;

        public CommonController(IDbService dbHelper, IysProxy iysClient, IIysHelper iysHelper)
        {
            _dbService = dbHelper;
            _client = iysClient;
            _iysHelper = iysHelper;
        }

        [Route("addConsent")]
        [HttpPost]
        public async Task<ResponseBase<AddConsentResult>> AddConsent([FromBody] AddConsentRequest request)
        {
            request.CompanyCode = _iysHelper.GetCompanyCode(request.IysCode);
            var response = await _client.PostJsonAsync<Consent, AddConsentResult>($"consents/{request.CompanyCode}/addConsent", request.Consent);

            if (!request.WithoutLogging)
            {
                var id = await _dbService.InsertConsentRequest(request);
                response.Id = id;
                await _dbService.UpdateConsentResponseFromCommon(response);
                response.OriginalError = null;
            }

            return response;
        }

        [Route("addConsentAsync")]
        [HttpPost]
        public async Task<int> AddConsentAsync([FromBody] AddConsentRequest request)
            => await _dbService.InsertConsentRequest(request);


        [Route("queryConsent")]
        [HttpPost]
        public async Task<ResponseBase<QueryConsentResult>> QueryConsent([FromBody] QueryConsentRequest request)
        {
            request.CompanyCode = _iysHelper.GetCompanyCode(request.IysCode);
            return await _client.PostJsonAsync<RecipientKey, QueryConsentResult>($"consents/{request.CompanyCode}/queryConsent", request.RecipientKey);
        }
           

        [Route("queryConsentAsync/{id}")]
        [HttpGet]
        public async Task<ResponseBase<AddConsentResult>> QueryConsentAsync(int id)
        {
            var consentLog = await _dbService.GetConsentRequest(id);

            var response = new ResponseBase<AddConsentResult>
            {
                Id = consentLog.Id,
                LogId = consentLog.LogId,
                SendDate = consentLog.SendDate
            };

            if (!consentLog.IsProcessed)
            {
                response.Status = ServiceResponseStatuses.Waiting;
            }
            else
            {
                if (consentLog.IsSuccess)
                {
                    response.Status = ServiceResponseStatuses.Success;
                    response.Data = JsonConvert.DeserializeObject<AddConsentResult>(consentLog.Response);
                }
                else
                {
                    response.Status = ServiceResponseStatuses.Error;
                    var error = JsonConvert.DeserializeObject<GenericError>(consentLog.Response);
                    if (!string.IsNullOrEmpty(error.Message))
                    {
                        response.AddMessage(error.Status.ToString(), error.Message);
                    }
                    else
                    {
                        if (error.Errors?.Length > 0)
                        {
                            foreach (var detail in error.Errors)
                            {
                                response.AddMessage(detail.Code, detail.Message);
                            }
                        }
                    }
                }
            }

            return response;
        }

        [Route("addMultipleConsent")]
        [HttpPost]
        public async Task<ResponseBase<MultipleConsentResult>> AddMultipleConsent([FromBody] MultipleConsentRequest request)
        {
            var count = 0;
            var requestCount = request.Consents.Count;
            var response = new ResponseBase<MultipleConsentResult>();

            foreach (var consent in request.Consents)
            {
                var companyCode = _iysHelper.GetCompanyCode(request.IysCode);
                var addConsentRequest = new AddConsentRequest
                {
                    CompanyCode = companyCode,
                    SalesforceId = consent.SalesforceId,
                    IysCode = request.IysCode,
                    BrandCode = request.BrandCode,
                    Consent = new Consent
                    {
                        ConsentDate = consent.ConsentDate,
                        Source = consent.Source,
                        Recipient = consent.Recipient,
                        RecipientType = consent.RecipientType,
                        Status = consent.Status,
                        Type = consent.Type,
                    }
                };

                var result = await _dbService.InsertConsentRequest(addConsentRequest);
                count += result;
            }

            response.AddMessage("Success", $"{count}/{requestCount} kayıt başarı ile eklendi");
            response.Success();
            return response;
        }

        [Route("sendMultipleConsent")]
        [HttpPost]
        public async Task<ResponseBase<MultipleConsentResult>> SendMultipleConsent([FromBody] MultipleConsentRequest request)
        {
            request.CompanyCode = _iysHelper.GetCompanyCode(request.IysCode);
            return await _client.PostJsonAsync<MultipleConsentRequest, MultipleConsentResult>($"consents/{request.CompanyCode}/sendMultipleConsent", request);
        }
          


        [Route("queryMultipleConsent")]
        [HttpPost]
        public async Task<ResponseBase<List<QueryMultipleConsentResult>>> QueryMultipleConsent(QueryMultipleConsentRequest request)
        {
            request.CompanyCode = _iysHelper.GetCompanyCode(request.IysCode);
            return await _client.PostJsonAsync<QueryMultipleConsentRequest, List<QueryMultipleConsentResult>>($"consents/{request.CompanyCode}/queryMultipleConsent", request);
        }



        [Route("pullConsent")]
        [HttpPost]
        public async Task<ResponseBase<PullConsentResult>> PullConsent(PullConsentRequest request)
        {
            request.CompanyCode = _iysHelper.GetCompanyCode(request.IysCode);
            return await _client.PostJsonAsync<PullConsentRequest, PullConsentResult>($"consents/{request.CompanyCode}/pullConsent", request);
        }
      


        [Route("sfaddconsent")]
        [HttpPost]
        public async Task<SfConsentAddResponse> SalesforceAddConsent(SfConsentAddRequest request)
        {
            var requestBody = new SfConsentAddRequest
            {
                Request = new SfConsentBase
                {
                    CompanyCode = request.Request.CompanyCode,
                    Consents = [.. request.Request.Consents.Select(x => new Consent
                    {
                        ConsentDate = x.ConsentDate,
                        Source = x.Source,
                        Recipient = x.Recipient,
                        RecipientType = x.RecipientType,
                        Status = x.Status,
                        Type = x.Type,
                    })]
                }
            };

            var logId = await _dbService.InsertLog(new IysRequest<SfConsentAddRequest>
            {
                Url = "/apexrest/iys",
                Body = requestBody,
                Action = "Salesforce Add Consent"
            });

            var response = await _client.PostJsonAsync<SfConsentAddRequest, SfConsentAddResponse>("salesForce/AddConsent", requestBody);

            await _dbService.UpdateLogFromResponseBase<SfConsentAddResponse>(response, logId);

            if (response.IsSuccessful())
            {
                var result = response.Data;
                result.LogId = logId;
                return result;
            }
            else
            {
                var errorResponse = JsonConvert.DeserializeObject<List<SfConsentAddErrorResponse>>("s");
                return new SfConsentAddResponse
                {
                    LogId = logId,
                    WsStatus = "ERROR",
                    WsDescription = $"{errorResponse.FirstOrDefault().errorCode}-{errorResponse.FirstOrDefault().message}"
                };
            }
        }

    }
}
