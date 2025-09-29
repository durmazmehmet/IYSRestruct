using IYS.Application.Services;
using IYS.Application.Services.Interface;
using IYS.Application.Services.Models.Base;
using IYS.Application.Services.Models.Error;
using IYS.Application.Services.Models.Request;
using IYS.Application.Services.Models.Response.Consent;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace IYS.Subscriber.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommonController : ControllerBase
    {
        private readonly IDbService _dbService;
        private readonly IIysProxy _client;
        private readonly IIysHelper _iysHelper;
        private readonly ErrorReportingService _sendConsentErrorService;
        private static readonly string[] QueryRecipientTypes = new[] { "BIREYSEL", "TACIR" };
        private static readonly string[] QueryConsentTypes = new[] { "ARAMA", "MESAJ", "EPOSTA" };

        public CommonController(
            IDbService dbHelper,
            IIysProxy iysClient,
            IIysHelper iysHelper,
            ErrorReportingService sendConsentErrorService
            )
        {
            _dbService = dbHelper;
            _client = iysClient;
            _iysHelper = iysHelper;
            _sendConsentErrorService = sendConsentErrorService;
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
            request.CompanyCode = _iysHelper.ResolveCompanyCode(request.CompanyCode, request.IysCode);

            var response = new ResponseBase<AddConsentResult>();

            if (request.Consent == null)
            {
                response.Error("QUEUE_FAILED", "Consent bilgisi zorunludur.");
                return response;
            }

            if (string.IsNullOrWhiteSpace(request.CompanyCode) && (request.IysCode == 0 || request.BrandCode == 0))
            {
                response.Error("QUEUE_FAILED", "Company bilgisi eksik (CompanyCode/IysCode/BrandCode).");
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

            if (request.IsForceSend)
            {
                var sendResponse = await _client.PostJsonAsync<Consent, AddConsentResult>($"consents/{request.CompanyCode}/addConsent", request.Consent);
                sendResponse.Id = queuedId;
                await _dbService.UpdateConsentResponseFromResponse(sendResponse);
                return sendResponse;
            }

            response.Id = queuedId;
            response.Success();
            response.AddMessage("Queued", "İzin isteği işlenmek üzere sıraya alındı.");

            return response;
        }

        /// <summary>
        /// Sf'dan gelen, aynı müşteri için birden fazla türde izin kaydının ileitlmesi için kullanılır.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Route("addMultipleConsent")]
        [HttpPost]
        public async Task<ResponseBase<MultipleConsentResult>> AddMultipleConsent([FromBody] MultipleConsentRequest request)
        {
            request.CompanyCode = _iysHelper.ResolveCompanyCode(request.CompanyCode, request.IysCode);

            var requestCount = request.Consents.Count;
            var response = new ResponseBase<MultipleConsentResult>();

            if ((request.IysCode == 0 || request.BrandCode == 0) && !string.IsNullOrWhiteSpace(request.CompanyCode))
            {
                var consentParams = _iysHelper.GetIysCode(request.CompanyCode);
                request.IysCode = consentParams.IysCode;
                request.BrandCode = consentParams.BrandCode;
            }

            var queuedCount = 0;

            for (var i = 0; i < request.Consents.Count; i++)
            {
                var consent = request.Consents[i];

                if (consent == null)
                {
                    response.Error();                 
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
                    queuedCount++;
                    continue;
                }

                response.AddMessage($"Error_{i + 1}", "İzin isteği sıraya alınamadı.");
            }

            response.AddMessage("Success", $"{queuedCount}/{requestCount} kayıt başarı ile sıraya alındı");

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
            => await _client.PostJsonAsync<RecipientKey, QueryConsentResult>($"consents/{request.CompanyCode}/queryConsent", request.RecipientKey);

        /// <summary>
        /// IYS'den çoklu izin sorgulama (tek seferde en fazla 1000 kayıt sorgulanabilir)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Route("queryMultipleConsent")]
        [HttpPost]
        public async Task<ResponseBase<MultipleQueryConsentResult>> QueryMultipleConsent([FromBody] QueryMutipleConsentRequest request)
            => await _client.PostJsonAsync<RecipientKeyWithList, MultipleQueryConsentResult>($"consents/{request.CompanyCode}/queryMultipleConsent", request.RecipientKeyWithList);

        /// <summary>
        /// IYSConsentRequest kaydındaki bilgileri kullanarak queryConsent çağrısı yapar.
        /// </summary>
        /// <param name="id">IYSConsentRequest tablosundaki kayıt numarası.</param>
        /// <returns></returns>
        [HttpGet("queryConsentByRequestId/{id:long}")]
        public async Task<ResponseBase<QueryConsentResult>> QueryConsentByRequestId(long id)
        {
            var response = new ResponseBase<QueryConsentResult>();

            if (id <= 0)
            {
                response.Error("INVALID_ID", "Geçerli bir izin isteği numarası belirtilmelidir.");
                return response;
            }

            var consentRequest = await _dbService.GetConsentRequestById(id);

            if (consentRequest is null)
            {
                response.Error("NOT_FOUND", $"Belirtilen kimliğe sahip IYSConsentRequest kaydı bulunamadı. (Id: {id})");
                return response;
            }

            var companyCode = _iysHelper.ResolveCompanyCode(consentRequest.CompanyCode, consentRequest.IysCode) ?? consentRequest.CompanyCode;

            if (string.IsNullOrWhiteSpace(companyCode))
            {
                response.Error("COMPANY_NOT_FOUND", "Kayıt için geçerli bir şirket kodu bulunamadı.");
                return response;
            }

            if (string.IsNullOrWhiteSpace(consentRequest.Recipient))
            {
                response.Error("RECIPIENT_NOT_FOUND", "Kayıtta sorgulanacak alıcı bilgisi bulunamadı.");
                return response;
            }

            if (string.IsNullOrWhiteSpace(consentRequest.RecipientType))
            {
                response.Error("RECIPIENT_TYPE_NOT_FOUND", "Kayıtta sorgulanacak alıcı tipi bilgisi bulunamadı.");
                return response;
            }

            if (string.IsNullOrWhiteSpace(consentRequest.Type))
            {
                response.Error("TYPE_NOT_FOUND", "Kayıtta sorgulanacak izin tipi bilgisi bulunamadı.");
                return response;
            }

            var queryResponse = await _client.PostJsonAsync<RecipientKey, QueryConsentResult>($"consents/{companyCode}/queryConsent", new RecipientKey
            {
                Recipient = consentRequest.Recipient,
                RecipientType = consentRequest.RecipientType,
                Type = consentRequest.Type
            });

            queryResponse.Id = consentRequest.Id;

            return queryResponse;
        }

        /// <summary>
        /// Belirtilen alıcı için tüm şirket kodları ve izin tipleri kombinasyonlarıyla queryConsent çağrısı yapar.
        /// </summary>
        /// <param name="recipient">E-posta adresi veya telefon numarası.</param>
        /// <returns></returns>
        [HttpGet("queryConsentByRecipient")]
        public async Task<ResponseBase<List<QueryConsentAggregationItem>>> QueryConsentByRecipient([FromQuery] string recipient)
        {
            var response = new ResponseBase<List<QueryConsentAggregationItem>>();

            if (string.IsNullOrWhiteSpace(recipient))
            {
                response.Error("RECIPIENT_REQUIRED", "Sorgulanacak alıcı bilgisi zorunludur.");
                return response;
            }

            var normalizedRecipient = recipient.Trim();

            var companyCodes = _iysHelper.GetAllCompanyCodes()
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => _iysHelper.ResolveCompanyCode(code, 0) ?? code.Trim())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (companyCodes.Count == 0)
            {
                response.Error("COMPANY_CODES_NOT_FOUND", "Sorgulanacak şirket kodu bulunamadı.");
                return response;
            }

            var aggregatedResults = new List<QueryConsentAggregationItem>(companyCodes.Count * QueryRecipientTypes.Length * QueryConsentTypes.Length);

            foreach (var companyCode in companyCodes)
            {
                foreach (var recipientType in QueryRecipientTypes)
                {
                    foreach (var type in QueryConsentTypes)
                    {
                        ResponseBase<QueryConsentResult> queryResult;

                        try
                        {
                            queryResult = await _client.PostJsonAsync<RecipientKey, QueryConsentResult>($"consents/{companyCode}/queryConsent", new RecipientKey
                            {
                                Recipient = normalizedRecipient,
                                RecipientType = recipientType,
                                Type = type
                            });
                        }
                        catch (Exception ex)
                        {
                            queryResult = new ResponseBase<QueryConsentResult>();
                            queryResult.Error("QUERY_CONSENT_FAILED", ex.Message);
                        }

                        aggregatedResults.Add(new QueryConsentAggregationItem
                        {
                            CompanyCode = companyCode,
                            RecipientType = recipientType,
                            Type = type,
                            Response = queryResult
                        });
                    }
                }
            }

            response.Success(aggregatedResults);
            response.AddMessage("CombinationCount", aggregatedResults.Count.ToString());

            return response;
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
            request.CompanyCode = _iysHelper.ResolveCompanyCode(request.CompanyCode, request.IysCode);
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

            await _dbService.UpdateLogFromResponseBase(response, logId);

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

        /// <summary>
        /// COnsent hatalarını tarih bazlı getirir.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        [HttpGet("GetErrorReport")]
        public async Task<IActionResult> GetConsentErrorJson([FromQuery] DateTime? date)
        {
            var result = await _sendConsentErrorService.GetErrorsJsonAsync(date);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

        [HttpGet("GetErrorReportStats")]
        public async Task<IActionResult> GetConsentErrorStats([FromQuery] DateTime? date)
        {
            var result = await _sendConsentErrorService.GetErrorReportStatsAsync(date);
            return StatusCode(result.IsSuccessful() ? 200 : 500, result);
        }

    }
}
