using IYSIntegration.Application.Services;
using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Error;
using IYSIntegration.Application.Services.Models.Request.Consent;
using IYSIntegration.Application.Services.Models.Response.Consent;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IYSIntegration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommonController : ControllerBase
    {
        private readonly IDbService _dbService;
        private readonly IysProxy _client;
        private readonly IIysHelper _iysHelper;
        public CommonController(
            IDbService dbHelper,
            IysProxy iysClient,
            IIysHelper iysHelper)
        {
            _dbService = dbHelper;
            _client = iysClient;
            _iysHelper = iysHelper;
        }

        /// <summary>
        /// Sf'dan gelen tekil izin ekleme istekleri için kullanılır.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Route("addConsent")]
        [HttpPost]
        public async Task<ResponseBase<AddConsentResult>> AddConsent([FromBody] AddConsentRequest request)
        {
            request.CompanyCode = _iysHelper.ResolveCompanyCode(request.CompanyCode, request.CompanyName, request.IysCode);

            var response = new ResponseBase<AddConsentResult>();
            var forceSend = _iysHelper.IsForceSendEnabled();

            if (request.Consent == null)
            {
                response.Error("QUEUE_FAILED", "Consent bilgisi zorunludur.");
                return response;
            }

            if ((request.IysCode == 0 || request.BrandCode == 0) && !string.IsNullOrWhiteSpace(request.CompanyCode))
            {
                var consentParams = _iysHelper.GetIysCode(request.CompanyCode);
                request.IysCode = consentParams.IysCode;
                request.BrandCode = consentParams.BrandCode;
            }

            var queuedId = await _dbService.InsertConsentRequest(request);

            if (queuedId <= 0)
            {
                response.Error("QUEUE_FAILED", "İzin isteği sıraya alınamadı.");
                return response;
            }

            if (forceSend)
            {
                var sendResponse = await _client.PostJsonAsync<Consent, AddConsentResult>($"consents/{request.CompanyCode}/addConsent", request.Consent);
                sendResponse.Id = queuedId;
                await _dbService.UpdateConsentResponseFromCommon(sendResponse);
                return sendResponse;
            }

            response.Id = queuedId;
            response.Success();
            response.AddMessage("Queued", "İzin isteği işlenmek üzere sıraya alındı.");

            return response;
        }

        /// <summary>
        /// Sf'dan gelen, aynı müşteri için birden fazla türde izin kaydının ileitlmek üzere
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Route("addMultipleConsent")]
        [HttpPost]
        public async Task<ResponseBase<MultipleConsentResult>> AddMultipleConsent([FromBody] MultipleConsentRequest request)
        {
            request.CompanyCode = _iysHelper.ResolveCompanyCode(request.CompanyCode, request.CompanyName, request.IysCode);

            var requestCount = request.Consents.Count;
            var response = new ResponseBase<MultipleConsentResult>();
            var forceSend = _iysHelper.IsForceSendEnabled();

            if ((request.IysCode == 0 || request.BrandCode == 0) && !string.IsNullOrWhiteSpace(request.CompanyCode))
            {
                var consentParams = _iysHelper.GetIysCode(request.CompanyCode);
                request.IysCode = consentParams.IysCode;
                request.BrandCode = consentParams.BrandCode;
            }

            var successCount = 0;
            var queuedCount = 0;
            var hasError = false;

            for (var i = 0; i < request.Consents.Count; i++)
            {
                var consent = request.Consents[i];

                if (consent == null)
                {
                    if (!hasError)
                    {
                        response.Error();
                        hasError = true;
                    }

                    response.AddMessage($"Error_{i + 1}", "Consent bilgisi zorunludur.");
                    continue;
                }

                var addConsentRequest = new AddConsentRequest
                {
                    CompanyCode = request.CompanyCode,
                    CompanyName = request.CompanyName,
                    IysCode = request.IysCode,
                    BrandCode = request.BrandCode,
                    SalesforceId = consent.SalesforceId,
                    Consent = new Consent
                    {
                        ConsentDate = consent.ConsentDate,
                        Source = consent.Source,
                        Recipient = consent.Recipient,
                        RecipientType = consent.RecipientType,
                        Status = consent.Status,
                        Type = consent.Type,
                        RetailerCode = consent.RetailerCode,
                        RetailerAccess = consent.RetailerAccess,
                        SalesforceId = consent.SalesforceId
                    }
                };

                var result = await _dbService.InsertConsentRequest(addConsentRequest);

                if (result > 0)
                {
                    if (forceSend)
                    {
                        var addResponse = await _client.PostJsonAsync<Consent, AddConsentResult>($"consents/{request.CompanyCode}/addConsent", consent);
                        addResponse.Id = result;
                        await _dbService.UpdateConsentResponseFromCommon(addResponse);

                        if (addResponse.IsSuccessful() && addResponse.HttpStatusCode >= 200 && addResponse.HttpStatusCode < 300)
                        {
                            successCount++;
                            continue;
                        }

                        if (!hasError)
                        {
                            response.Error();
                            hasError = true;
                        }

                        response.AddMessage($"Error_{i + 1}", _iysHelper.BuildAddConsentErrorMessage(addResponse));
                        continue;
                    }

                    queuedCount++;
                    continue;
                }

                if (!hasError)
                {
                    response.Error();
                    hasError = true;
                }

                response.AddMessage($"Error_{i + 1}", "İzin isteği sıraya alınamadı.");
            }

            if (forceSend)
            {
                response.AddMessage("Success", $"{successCount}/{requestCount} kayıt başarı ile gönderildi");
            }
            else
            {
                response.AddMessage("Success", $"{queuedCount}/{requestCount} kayıt başarı ile sıraya alındı");
            }

            if (!hasError)
            {
                response.Success();
            }

            return response;
        }

        /// <summary>
        /// IYS'den tekil izin sorgulama (1 saatde en fazla 1000 sorgu yapılabilir)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Route("queryConsent")]
        [HttpPost]
        public async Task<ResponseBase<QueryConsentResult>> QueryConsent([FromBody] QueryConsentRequest request)
        {
            request.CompanyCode = _iysHelper.ResolveCompanyCode(request.CompanyCode, null, request.IysCode);
            return await _client.PostJsonAsync<RecipientKey, QueryConsentResult>($"consents/{request.CompanyCode}/queryConsent", request.RecipientKey);
        }

        /// <summary>
        /// IYS'den çoklu izin sorgulama (tek seferde en fazla 1000 kayıt sorgulanabilir)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Route("queryMultipleConsent")]
        [HttpPost]
        public async Task<ResponseBase<MultipleQueryConsentResult>> QueryMultipleConsent([FromBody] QueryMutipleConsentRequest request)
        {
            request.CompanyCode = _iysHelper.ResolveCompanyCode(request.CompanyCode, null, request.IysCode);
            return await _client.PostJsonAsync<RecipientKeyWithList, MultipleQueryConsentResult>($"consents/{request.CompanyCode}/queryMultipleConsent", request.RecipientKeyWithList);
        }

        /// <summary>
        /// IYSConsentRequest ve IYSCallLog sorgulanır
        /// </summary>
        /// <param name="recipient"></param>
        /// <returns></returns>
        [Route("queryConsentLog/{recepient}")]
        [HttpGet]
        public async Task<ResponseBase<AddConsentResult>> QueryConsentLog(string recipient)
        {
            var consentLog = await _dbService.GetConsentRequest(recipient);

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
                
        /// <summary>
        /// IYS'den consent çekimi için kullanılır.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Route("pullConsent")]
        [HttpPost]
        public async Task<ResponseBase<PullConsentResult>> PullConsent(PullConsentRequest request)
        {
            request.CompanyCode = _iysHelper.ResolveCompanyCode(request.CompanyCode, null, request.IysCode);

            return await _client.PostJsonAsync<PullConsentRequest, PullConsentResult>($"consents/{request.CompanyCode}/pullConsent", request);
        }


        /// <summary>
        /// Sf'a gönderilecek toplu izin ekleme istekleri için kullanılır.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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
