using IYSIntegration.Application.Middleware.LoggingService;
using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Error;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;

namespace IYSIntegration.Application.Services
{
    public class IysRestClientService : IIysRestClientService
    {
        private readonly IIysIdentityService _identityManager;
        private readonly IDbService _dbService;
        private readonly LoggerServiceBase loggerService;

        public IysRestClientService(IIysIdentityService identityManager, IDbService dbHelper, LoggerServiceBase loggerService)
        {
            _identityManager = identityManager;
            _dbService = dbHelper;
            this.loggerService = loggerService;
        }

        public async Task<ResponseBase<TResponse>> Execute<TResponse, TRequest>(IysRequest<TRequest> iysRequest)
        {
            var logId = await _dbService.InsertLog(iysRequest);

            var response = new ResponseBase<TResponse>
            {
                Status = ServiceResponseStatuses.Error,
                LogId = logId
            };

            var token = await _identityManager.GetToken(iysRequest.IysCode, false);

            var httpResponse = await GetReponse(iysRequest, token.AccessToken);

            if ((int)httpResponse.StatusCode == 401 || (int)httpResponse.StatusCode == 403)
            {
                token = await _identityManager.GetToken(iysRequest.IysCode, true);

                httpResponse = await GetReponse(iysRequest, token.AccessToken);
            }

            await _dbService.UpdateLog(httpResponse, logId);

            response.SendDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            response.HttpStatusCode = (int)httpResponse.StatusCode;

            if (httpResponse.IsSuccessful)
            {
                response.Success(JsonConvert.DeserializeObject<TResponse>(httpResponse.Content));
            }
            else
            {
                response.AddMessage("Url:", iysRequest.Url);
                try
                {
                    var error = JsonConvert.DeserializeObject<GenericError>(httpResponse.Content);
                    response.OriginalError = error;
                    if (error == null)
                    {
                        loggerService.Error($"Unexpected error occured: {httpResponse.Content}");
                        response.AddMessage("Error", "Unexpected error");
                    }
                    else if (!string.IsNullOrEmpty(error.Message))
                    {
                        loggerService.Error($"{error.Status}, {error.Message}");
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
                catch (Exception ex)
                {
                    loggerService.Error($"Unexpected error occured. {ex.Message}");
                    response.Error(response.HttpStatusCode.ToString(), httpResponse.Content);
                }
            }

            return response;
        }

        private async Task<RestResponse> GetReponse<TRequest>(IysRequest<TRequest> iysRequest, string accessToken)
        {
            var httpRequest = new RestRequest();
            var bodyContent = string.Empty;
            var responseBodyWithMaskedRecepient = string.Empty;
            var requestBodyWithMaskedRecepient = string.Empty;
            var responseContent = string.Empty;

            var client = new RestClient(new RestClientOptions(iysRequest.Url)
            {
                Authenticator = new JwtAuthenticator(accessToken)
            });

            if (iysRequest.Body != null)
            {
                bodyContent = JsonConvert.SerializeObject(
                    iysRequest.Body,
                    Formatting.None,
                    new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }
                    );

                httpRequest.AddParameter("application/json", bodyContent, ParameterType.RequestBody);
            }

            RestResponse httpResponse = new();

            switch (iysRequest.Method)
            {
                case Method.Post:
                    httpResponse = await client.ExecutePostAsync(httpRequest);
                    break;
                case Method.Get:
                    httpResponse = await client.ExecuteGetAsync(httpRequest);
                    break;
                case Method.Put:
                    httpResponse = await client.ExecutePutAsync(httpRequest);
                    break;
                case Method.Delete:
                    httpResponse = await client.ExecuteDeleteAsync(httpRequest);
                    break;
                case Method.Options:
                    httpResponse = await client.ExecuteOptionsAsync(httpRequest);
                    break;
                case Method.Patch:
                    httpResponse = await client.ExecutePatchAsync(httpRequest);
                    break;
                case Method.Head:
                    httpResponse = await client.ExecutePatchAsync(httpRequest);
                    break;
                default:
                    httpResponse = await client.ExecuteGetAsync(httpRequest);
                    break;
            }

            try
            {
                if (httpResponse.StatusCode != HttpStatusCode.OK || httpResponse.StatusCode == HttpStatusCode.NoContent)
                {
                    requestBodyWithMaskedRecepient = MaskRecipientInJson(bodyContent);

                    if (httpResponse.ContentType == "application/json")
                    {
                        responseContent = httpResponse.Content;
                        responseBodyWithMaskedRecepient = MaskRecipientInJson(responseContent);
                    }

                    string maskedToken = MaskString(accessToken);

                    loggerService.Error(
                    $"Action: {iysRequest.Action} {Environment.NewLine}"
                    + $"Method: {iysRequest.Method} {Environment.NewLine}"
                    + $"Url: {client.BuildUri(httpRequest)} {Environment.NewLine}"
                    + $"Token: {maskedToken} {Environment.NewLine}"
                    + $"Request Body: {requestBodyWithMaskedRecepient} {Environment.NewLine}"
                    + $"Response Status: {(int)httpResponse.StatusCode} {Environment.NewLine}"
                    + $"Response Body: {responseBodyWithMaskedRecepient}"
                    );
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"hata toplarken hata {ex.Message}");
            }

            return httpResponse;
        }

        private static string MaskString(string input) =>
            input.Length >= 9 ? input.Substring(0, 3) + "***" + input.Substring(input.Length - 3) : input;

        private static string MaskRecipientInJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return json;
            }

            var token = JToken.Parse(json);

            if (token.Type == JTokenType.Object)
            {
                MaskRecipientInJObject((JObject)token);
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var item in (JArray)token)
                {
                    if (item is JObject obj)
                    {
                        MaskRecipientInJObject(obj);
                    }
                }
            }

            return token.ToString(Formatting.None);
        }

        private static void MaskRecipientInJObject(JObject jsonObject)
        {
            if (jsonObject["recipient"] != null)
            {
                string recipient = jsonObject["recipient"]!.ToString();
                jsonObject["recipient"] = MaskString(recipient);
            }
        }
    }
}


