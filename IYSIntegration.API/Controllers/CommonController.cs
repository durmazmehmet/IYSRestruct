using IYSIntegration.Application.Interface;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.Error;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Common.Response.Consent;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace IYSIntegration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommonController : ControllerBase
    {
        private readonly IConsentService _consentManager;
        private readonly ISfConsentService _SfconsentManager;
        private readonly IDbService _dbService;
        private readonly IConfiguration _config;
        private object obj = new Object();

        public CommonController(IConsentService consentManager, IDbService dbHelper, IConfiguration config, ISfConsentService SfconsentManager)
        {
            _consentManager = consentManager;
            _dbService = dbHelper;
            _config = config;
            _SfconsentManager = SfconsentManager;
        }

        [Route("addConsent")]
        [HttpPost]
        public async Task<ResponseBase<AddConsentResult>> AddConsent([FromBody] AddConsentRequest request)
        {
            if (request.IysCode == 0)
            {
                var consentParams = _consentManager.GetIysCode(request.CompanyCode);
                request.IysCode = consentParams.IysCode;
                request.BrandCode = consentParams.BrandCode;
            }

            if (!request.WithoutLogging)
            {
                var id = await _dbService.InsertConsentRequest(request);
                var response = await _consentManager.AddConsent(request);
                response.Id = id;
                await _dbService.UpdateConsentResponseFromCommon(response);
                response.OriginalError = null;
                return response;
            }
            else
            {
                return await _consentManager.AddConsent(request);
            }
        }

        [Route("addConsentAsync")]
        [HttpPost]
        public async Task<int> AddConsentAsync([FromBody] AddConsentRequest request)
        {
            if (request.IysCode == 0)
            {
                var consentParams = _consentManager.GetIysCode(request.CompanyCode);
                request.IysCode = consentParams.IysCode;
                request.BrandCode = consentParams.BrandCode;
            }

            return await _dbService.InsertConsentRequest(request);
        }

        [Route("queryConsent")]
        [HttpPost]
        public async Task<ResponseBase<QueryConsentResult>> QueryConsent([FromBody] QueryConsentRequest request)
        {
            if (request.IysCode == 0)
            {
                var consentParams = _consentManager.GetIysCode(request.CompanyCode);
                request.IysCode = consentParams.IysCode;
                request.BrandCode = consentParams.BrandCode;
            }

            var response = await _consentManager.QueryConsent(request);
            return response;
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
                response.Status = Common.Base.ServiceResponseStatuses.Waiting;
            }
            else
            {
                if (consentLog.IsSuccess)
                {
                    response.Status = Common.Base.ServiceResponseStatuses.Success;
                    response.Data = JsonConvert.DeserializeObject<AddConsentResult>(consentLog.Response);
                }
                else
                {
                    response.Status = Common.Base.ServiceResponseStatuses.Error;
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
            var response = new ResponseBase<MultipleConsentResult>();

            if (request.IysCode == 0)
            {
                var consentParams = _consentManager.GetIysCode(request.CompanyCode);
                request.IysCode = consentParams.IysCode;
                request.BrandCode = consentParams.BrandCode;
            }

            foreach (var consent in request.Consents)
            {
                var addConsentRequest = new AddConsentRequest
                {
                    CompanyCode = request.CompanyCode,
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

                await _dbService.InsertConsentRequest(addConsentRequest);
            }

            response.Success();
            return response;
        }

        [Route("sendMultipleConsent")]
        [HttpPost]
        public async Task<ResponseBase<MultipleConsentResult>> SendMultipleConsent([FromBody] MultipleConsentRequest request)
        {
            if (request.IysCode == 0)
            {
                var consentParams = _consentManager.GetIysCode(request.CompanyCode);
                request.IysCode = consentParams.IysCode;
                request.BrandCode = consentParams.BrandCode;
            }

            var response = await _consentManager.AddMultipleConsent(request);
            return response;
        }


        [Route("queryMultipleConsent")]
        [HttpPost]
        public async Task<ResponseBase<List<QueryMultipleConsentResult>>> QueryMultipleConsent(QueryMultipleConsentRequest request)
        {
            if (request.IysCode == 0)
            {
                var consentParams = _consentManager.GetIysCode(request.CompanyCode);
                request.IysCode = consentParams.IysCode;
                request.BrandCode = consentParams.BrandCode;
            }

            return await _consentManager.QueryMultipleConsent(request);
        }


        [Route("pullConsent")]
        [HttpPost]
        public async Task<ResponseBase<PullConsentResult>> PullConsent(PullConsentRequest request)
        {
            if (request.IysCode == 0)
            {
                var consentParams = _consentManager.GetIysCode(request.CompanyCode);
                request.IysCode = consentParams.IysCode;
                request.BrandCode = consentParams.BrandCode;
            }

            return await _consentManager.PullConsent(request);
        }

        [Route("sfaddconsent")]
        [HttpPost]
        public async Task<SfConsentAddResponse> SalesforceAddConsent(SfConsentAddRequest request)
        {
            return await _SfconsentManager.AddConsent(request);
        }

       
    }
}
