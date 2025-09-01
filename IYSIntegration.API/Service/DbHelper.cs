using Dapper;
using IYSIntegration.API.Constanst;
using IYSIntegration.API.Interface;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Common.Response.Consent;
using Newtonsoft.Json;
using RestSharp;
using System.Data.SqlClient;

namespace IYSIntegration.API.Service
{
    public class DbHelper : IDbHelper
    {
        private readonly IConfiguration _configuration;

        public DbHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<int> InsertLog<TRequest>(IysRequest<TRequest> request)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteScalarAsync<int>(QueryStrings.InsertRequest,
                    new
                    {
                        IysCode = request.IysCode,
                        Action = request.Action,
                        Url = request.Url,
                        Method = request.Method.ToString(),
                        Request = request.Body == null ? string.Empty : JsonConvert.SerializeObject(request.Body,
                                         Formatting.None,
                                         new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }),
                        BatchId = request.BatchId
                    });
                connection.Close();

                return result;
            }
        }

        public async Task UpdateLog(RestResponse response, int id)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteAsync(QueryStrings.UpdateRequest,
                    new
                    {
                        Id = id,
                        Response = response.Content,
                        IsSuccess = response.IsSuccessful ? 1 : 0,
                        ResponseCode = (int)response.StatusCode
                    });
                connection.Close();
            }
        }

        public async Task<int> InsertConsentRequest(AddConsentRequest request)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var lastConsent = await connection.QueryFirstOrDefaultAsync<ConsentRequestLog>(QueryStrings.GetLastConsentRequest,
                    new { CompanyCode = request.CompanyCode, Recipient = request.Consent.Recipient });

                if (lastConsent != null &&
                    lastConsent.IysCode == request.IysCode &&
                    lastConsent.BrandCode == request.BrandCode &&
                    lastConsent.ConsentDate == request.Consent.ConsentDate &&
                    lastConsent.Source == request.Consent.Source &&
                    lastConsent.RecipientType == request.Consent.RecipientType &&
                    lastConsent.Status == request.Consent.Status &&
                    lastConsent.Type == request.Consent.Type)
                {
                    connection.Close();
                    return 0;
                }

                var result = await connection.ExecuteScalarAsync<int>(QueryStrings.InsertConsentRequest,
                    new
                    {
                        CompanyCode = request.CompanyCode,
                        SalesforceId = request.SalesforceId,
                        IysCode = request.IysCode,
                        BrandCode = request.BrandCode,
                        ConsentDate = request.Consent.ConsentDate,
                        Source = request.Consent.Source,
                        Recipient = request.Consent.Recipient,
                        RecipientType = request.Consent.RecipientType,
                        Status = request.Consent.Status,
                        Type = request.Consent.Type
                    });
                connection.Close();

                return result;
            }
        }

        public async Task UpdateConsentResponse(ResponseBase<AddConsentResult> response)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteAsync(QueryStrings.UpdateConsentRequest,
                    new
                    {
                        Id = response.Id,
                        LogId = response.LogId,
                        IsSuccess = response.IsSuccessful() ? 1 : 0,
                        TransactionId = response.Data?.TransactionId,
                        CreationDate = response.Data?.CreationDate
                    }); ;
                connection.Close();
            }
        }

        public async Task<ConsentResultLog> GetConsentRequest(long id)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = (await connection.QueryAsync<ConsentResultLog>(QueryStrings.QueryConsentRequest,
                    new
                    {
                        Id = id
                    })).SingleOrDefault();

                connection.Close();

                return result;
            }
        }

        public async Task<int> GetMaxBatchId()
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteScalarAsync<int>(QueryStrings.GetMaxBatchId);
                connection.Close();

                return result;
            }
        }

        public async Task<int> InsertConsentRequestWithBatch(AddConsentRequest request)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteScalarAsync<int>(QueryStrings.InsertConsentRequestWitBatch,
                    new
                    {
                        CompanyCode = request.CompanyCode,
                        SalesforceId = request.SalesforceId,
                        IysCode = request.IysCode,
                        BrandCode = request.BrandCode,
                        ConsentDate = request.Consent.ConsentDate,
                        Source = request.Consent.Source,
                        Recipient = request.Consent.Recipient,
                        RecipientType = request.Consent.RecipientType,
                        Status = request.Consent.Status,
                        Type = request.Consent.Type,
                        BatchId = request.Consent.BatchId,
                        Index = request.Consent.Index,
                        LogId = request.Consent.LogId,
                        IsSuccess = request.Consent.IsSuccess,
                        BatchError = request.Consent.BatchError
                    });
                connection.Close();

                return result;
            }
        }

        public async Task InsertBatchConsentQuery(BatchConsentQuery request)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteScalarAsync<int>(string.Format(QueryStrings.InsertMultipleConsentQuery, request.CheckAfter),
                    new
                    {
                        IysCode = request.IysCode,
                        BrandCode = request.BrandCode,
                        BatchId = request.BatchId,
                        LogId = request.LogId,
                        RequestId = request.RequestId
                    });
                connection.Close();
            }
        }
    }
}
