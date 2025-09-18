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

        /// <summary>
        /// Sf'dan gelen tekil izin ekleme istekleri için kullanılır.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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

            var response = await _client.PostJsonAsync<Consent, AddConsentResult>($"consents/{request.CompanyCode}/addConsent", request.Consent);

            await _iysHelper.LogConsentAsync(
                request,
                response,
                shouldRunPendingSync);

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

        /// <summary>
        /// IYS'den tekil izin sorgulama
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
        /// Çoklu izin ekleme tarihçesini verir.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Route("queryAddMultipleConsent")]
        [HttpPost]
        public async Task<ResponseBase<List<QueryMultipleConsentResult>>> QueryAddMultipleConsent(QueryMultipleConsentRequest request)
        {
            request.CompanyCode = _iysHelper.ResolveCompanyCode(request.CompanyCode, null, request.IysCode);
            return await _client.GetAsync<List<QueryMultipleConsentResult>>($"consents/{request.CompanyCode}/queryAddMultipleConsent?requestId={request.RequestId}&batchId={request.BatchId}");
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
