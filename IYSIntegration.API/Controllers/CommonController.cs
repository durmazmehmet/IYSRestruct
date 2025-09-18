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
using System.Linq;
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
        private readonly IPendingSyncService _pendingSyncService;

        public CommonController(
            IDbService dbHelper,
            IysProxy iysClient,
            IIysHelper iysHelper,
            IPendingSyncService pendingSyncService)
        {
            _dbService = dbHelper;
            _client = iysClient;
            _iysHelper = iysHelper;
            _pendingSyncService = pendingSyncService;
        }

        [Route("addConsent")]
        [HttpPost]
        public async Task<ResponseBase<AddConsentResult>> AddConsent([FromBody] AddConsentRequest request)
        {
            var (isValid, validationResponse) = await _iysHelper.ValidateConsentRequestAsync(request);

            if (!isValid)
            {
                return validationResponse;
            }

            var consent = request.Consent;
            var shouldRunPendingSync = true;

            if (consent != null
                && !string.IsNullOrWhiteSpace(consent.Recipient)
                && !string.IsNullOrWhiteSpace(request.CompanyCode))
            {
                var hasPullRecord = await _dbService.PullConsentExists(request.CompanyCode, consent.Recipient, consent.Type);
                var hasSuccessfulRequest = await _dbService.SuccessfulConsentRequestExists(request.CompanyCode, consent.Recipient, consent.Type);

                if (!hasPullRecord && !hasSuccessfulRequest)
                {
                    const string syncMessageKey = "CONSENT_NOT_SYNCHRONIZED";
                    const string syncMessage = "İzin kaydı IYS üzerinden bulunamadığı için senkronizasyon başlatıldı.";

                    if (!request.WithoutLogging)
                    {
                        var loggedId = await _iysHelper.LogConsentRequestAsync(request);

                        var syncResponse = new ResponseBase<AddConsentResult>();

                        if (loggedId > 0)
                        {
                            syncResponse.Id = loggedId;
                        }

                        syncResponse.Error(syncMessageKey, syncMessage);
                        return syncResponse;
                    }

                    var pendingConsent = new Consent
                    {
                        Id = consent.Id,
                        CompanyCode = request.CompanyCode,
                        Recipient = consent.Recipient,
                        RecipientType = consent.RecipientType,
                        Type = consent.Type,
                        Status = consent.Status,
                        Source = consent.Source,
                        ConsentDate = consent.ConsentDate
                    };

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _pendingSyncService.SyncAsync(new[] { pendingConsent }).ConfigureAwait(false);
                        }
                        catch
                        {
                            // arka planda tetiklenen senkronizasyon hataları isteği engellememelidir.
                        }
                    });

                    var syncResponseWithoutLogging = new ResponseBase<AddConsentResult>();
                    syncResponseWithoutLogging.Error(syncMessageKey, syncMessage);
                    return syncResponseWithoutLogging;
                }

                shouldRunPendingSync = hasPullRecord;
            }

            if (request.WithoutLogging)
            {
                return await _client.PostJsonAsync<Consent, AddConsentResult>($"consents/{request.CompanyCode}/addConsent", request.Consent);
            }

            var response = await _client.PostJsonAsync<Consent, AddConsentResult>($"consents/{request.CompanyCode}/addConsent", request.Consent);

            await _iysHelper.LogConsentAsync(
                request,
                response,
                shouldRunPendingSync);

            return response;
        }



        [Route("queryConsent")]
        [HttpPost]
        public async Task<ResponseBase<QueryConsentResult>> QueryConsent([FromBody] QueryConsentRequest request)
        {
            request.CompanyCode = _iysHelper.ResolveCompanyCode(request.CompanyCode, null, request.IysCode);
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
            var requestCount = request.Consents.Count;
            var response = new ResponseBase<MultipleConsentResult>();
            var validatedConsents = await _iysHelper.ValidateMultipleConsentsAsync(request);
            var successCount = 0;
            var hasError = false;

            foreach (var consentResult in validatedConsents)
            {
                if (!consentResult.IsValid)
                {
                    if (!hasError)
                    {
                        response.Error();
                        hasError = true;
                    }

                    _iysHelper.AppendValidationMessages(response, consentResult);
                    continue;
                }

                var result = await _iysHelper.LogConsentRequestAsync(
                    consentResult.Request);

                if (result > 0 && consentResult.Request.Consent != null)
                {
                    successCount++;
                }
            }

            response.AddMessage("Success", $"{successCount}/{requestCount} kayıt başarı ile eklendi");

            if (!hasError)
            {
                response.Success();
            }

            return response;
        }

        [Route("sendMultipleConsent")]
        [HttpPost]
        public async Task<ResponseBase<MultipleConsentResult>> SendMultipleConsent([FromBody] MultipleConsentRequest request)
        {
            request.CompanyCode = _iysHelper.ResolveCompanyCode(request.CompanyCode, request.CompanyName, request.IysCode);

            return await _client.PostJsonAsync<MultipleConsentRequest, MultipleConsentResult>($"consents/{request.CompanyCode}/sendMultipleConsent", request);
        }
          


        [Route("queryMultipleConsent")]
        [HttpPost]
        public async Task<ResponseBase<List<QueryMultipleConsentResult>>> QueryMultipleConsent(QueryMultipleConsentRequest request)
        {
            request.CompanyCode = _iysHelper.ResolveCompanyCode(request.CompanyCode, null, request.IysCode);

            return await _client.PostJsonAsync<QueryMultipleConsentRequest, List<QueryMultipleConsentResult>>($"consents/{request.CompanyCode}/queryMultipleConsent", request);
        }


        [Route("searchRequestDetailsV2")]
        [HttpPost]
        public async Task<ResponseBase<List<QueryMultipleConsentResultV2>>> SearchRequestDetailsV2([FromBody] QueryMultipleConsentRequestV2 request)
        {
            if (string.IsNullOrEmpty(request.CompanyCode))
                request.CompanyCode = _iysHelper.GetCompanyCode(request.IysCode);

            return await _client.PostJsonAsync<QueryMultipleConsentRequestV2, List<QueryMultipleConsentResultV2>>($"consents/{request.CompanyCode}/searchRequestDetailsV2", request);
        }



        [Route("pullConsent")]
        [HttpPost]
        public async Task<ResponseBase<PullConsentResult>> PullConsent(PullConsentRequest request)
        {
            request.CompanyCode = _iysHelper.ResolveCompanyCode(request.CompanyCode, null, request.IysCode);

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
