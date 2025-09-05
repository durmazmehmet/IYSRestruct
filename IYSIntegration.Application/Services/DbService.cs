using Dapper;
using IYSIntegration.Application.Base;
using IYSIntegration.Application.Request.Consent;
using IYSIntegration.Application.Response.Consent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System.Data.SqlClient;
using System;
using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Constants;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Helpers;
namespace IYSIntegration.Application.Services
{
    public class DbService : IDbService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DbService> _logger;
        private readonly IIysHelper _iysHelper;

        public DbService(IConfiguration configuration, ILogger<DbService> loggerServiceBase, IIysHelper iysHelper   )
        {
            _configuration = configuration;
            _logger = loggerServiceBase;
            _iysHelper = iysHelper;
        }

        public async Task<int> InsertLog<TRequest>(IysRequest<TRequest> request)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteScalarAsync<int>(QueryStrings.InsertRequest,
                    new
                    {
                        request.IysCode,
                        request.Action,
                        request.Url,
                        Method = request.Method.ToString(),
                        Request = request.Body == null ? string.Empty : JsonConvert.SerializeObject(request.Body,
                                         Formatting.None,
                                         new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }),
                        request.BatchId
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

        public async Task<T> UpdateLogFromResponseBase<T>(ResponseBase<T> response, int id)
        {
            var connStr = _configuration.GetValue<string>("ConnectionStrings:SfdcMasterData");

            using (var connection = new SqlConnection(connStr))
            {
                await connection.OpenAsync();

                var result = await connection.ExecuteAsync(QueryStrings.UpdateRequest, new
                {
                    Id = id,
                    Response = JsonConvert.SerializeObject(response.Data),
                    IsSuccess = response.IsSuccessful() ? 1 : 0,
                    ResponseCode = response.HttpStatusCode
                });

                return response.Data;
            }
        }


        public async Task<int> InsertConsentRequest(AddConsentRequest request)
        {
            if (request.IysCode == 0)
            {
                var consentParams = _iysHelper.GetIysCode(request.CompanyCode);
                request.IysCode = consentParams.IysCode;
                request.BrandCode = consentParams.BrandCode;
            }

            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var lastConsent = await connection.QueryFirstOrDefaultAsync<ConsentRequestLog>(QueryStrings.GetLastConsentRequest,
                    new { request.CompanyCode, request.Consent.Recipient });

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
                        request.CompanyCode,
                        request.SalesforceId,
                        request.IysCode,
                        request.BrandCode,
                        request.Consent.ConsentDate,
                        request.Consent.Source,
                        request.Consent.Recipient,
                        request.Consent.RecipientType,
                        request.Consent.Status,
                        request.Consent.Type
                    });
                connection.Close();

                return result;
            }
        }

        public async Task UpdateConsentResponseFromCommon(ResponseBase<AddConsentResult> response)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteAsync(QueryStrings.UpdateConsentRequestFromCommon,
                    new
                    {
                        response.Id,
                        response.LogId,
                        IsSuccess = response.IsSuccessful() ? 1 : 0,
                        response.Data?.TransactionId,
                        response.Data?.CreationDate
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
                        request.CompanyCode,
                        request.SalesforceId,
                        request.IysCode,
                        request.BrandCode,
                        request.Consent.ConsentDate,
                        request.Consent.Source,
                        request.Consent.Recipient,
                        request.Consent.RecipientType,
                        request.Consent.Status,
                        request.Consent.Type,
                        request.Consent.BatchId,
                        request.Consent.Index,
                        request.Consent.LogId,
                        request.Consent.IsSuccess,
                        request.Consent.BatchError
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
                        request.IysCode,
                        request.BrandCode,
                        request.BatchId,
                        request.LogId,
                        request.RequestId
                    });
                connection.Close();
            }
        }

        public async Task<List<BatchSummary>> GetBatchSummary(int batchCount)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = (await connection.QueryAsync<BatchSummary>(string.Format(QueryStrings.GetBatchSummary, batchCount))).ToList();
                connection.Close();
                return result;
            }
        }

        public async Task<List<ConsentRequestLog>> GetConsentRequests(bool isProcessed, int rowCount)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = (await connection.QueryAsync<ConsentRequestLog>(string.Format(QueryStrings.GetConsentRequests, rowCount),
                    new
                    {
                        IsProcessed = isProcessed ? 1 : 0
                    })).ToList();

                connection.Close();

                return result;
            }

        }

        public async Task UpdateBatchId(string companyCode, int batchSize)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                await connection.ExecuteAsync(string.Format(QueryStrings.UpdateBatchId, companyCode, batchSize));
                connection.Close();
            }
        }

        public async Task UpdateConsentResponse(ResponseBase<AddConsentResult> response)
        {
            var errorCodeList = new List<string>();
            var overdueErrors = new List<string> { "H174", "H175", "H178" };

            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                bool isConsentOverdue = response.OriginalError.Errors.Any(x => overdueErrors.Contains(x.Code));

                errorCodeList = response.OriginalError?.Errors?.Select(x => x.Code).ToList();

                var errors = "Mevcut olmayan";

                if (errorCodeList != null && errorCodeList.Count > 0) errors = string.Join(",", errorCodeList);

                if (response.HttpStatusCode == 200)
                {
                    _logger.LogInformation($"SingleConsentWorker ID {response.Id} ve {(int)response.HttpStatusCode} statu olarak alındı");
                }
                else if (errorCodeList.Any(x => overdueErrors.Contains(x)))
                {
                    _logger.LogWarning($"SingleConsentWorker ID {response.Id} ve IYS geciken/mükerrer {errors} olarak alındı");
                }
                else
                {
                    _logger.LogError($"SingleConsentWorker ID {response.Id} ve {(int)response.HttpStatusCode} statu kodu ve {errors} IYS hataları ile alınamadı");
                }

                connection.Open();
                var result = await connection.ExecuteAsync(QueryStrings.UpdateConsentRequest,
                    new
                    {
                        Id = response.Id,
                        LogId = response.LogId,
                        IsSuccess = isConsentOverdue ? 0 : response.IsSuccessful() ? 1 : 0,
                        TransactionId = response.Data?.TransactionId,
                        CreationDate = response.Data?.CreationDate,
                        BatchError = response.OriginalError == null ? null : JsonConvert.SerializeObject(response.OriginalError, Formatting.None, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }),
                        IsOverdue = isConsentOverdue ? 1 : 0
                    });
                connection.Close();
            }
        }

        public async Task<List<ConsentRequestLog>> GeBatchConsentRequests(int batchId)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = (await connection.QueryAsync<ConsentRequestLog>(QueryStrings.GetBatchConsentRequests,
                    new
                    {
                        BatchId = batchId
                    })).ToList();

                connection.Close();

                return result;
            }
        }

        public async Task UpdateBatchConsentRequests(BatchConsentQuery query)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteAsync(string.Format(QueryStrings.UpdateBatchConsentRequests, query.CheckAfter),
                    new
                    {
                        IysCode = query.IysCode,
                        BrandCode = query.BrandCode,
                        BatchId = query.BatchId,
                        LogId = query.LogId,
                        RequestId = query.RequestId
                    });
                connection.Close();
            }
        }

        public async Task<List<BatchConsentQuery>> GetUnprocessedMultipleConsenBatches(int batchCount)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = (await connection.QueryAsync<BatchConsentQuery>(string.Format(QueryStrings.GetUnprocessedMultipleConsenBatches, batchCount))).ToList();
                connection.Close();

                return result;
            }
        }

        public async Task UpdateMultipleConsentQueryDate(int batchId, long logId)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteAsync(QueryStrings.UpdateMultipleConsentQueryDate,
                    new
                    {
                        LogId = logId,
                        BatchId = batchId
                    });
                connection.Close();
            }
        }

        public async Task UpdateMultipleConsentItem(BatchItemResult batchItemResult)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteAsync(QueryStrings.UpdateMultipleConsentItem,
                    new
                    {
                        BatchId = batchItemResult.BatchId,
                        Index = batchItemResult.Index,
                        IsSuccess = batchItemResult.IsSuccess,
                        BatchError = batchItemResult.BatchError,
                        LogId = batchItemResult.LogId,
                        IsQueryResult = batchItemResult.IsQueryResult
                    });
                connection.Close();
            }
        }

        public async Task ReorderBatch(int oldBatchId)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteAsync(QueryStrings.ReorderBatch,
                    new
                    {
                        OldBatchId = oldBatchId
                    });
                connection.Close();
            }
        }

        public async Task<PullRequestLog> GetPullRequestLog(string companyCode)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = (await connection.QueryAsync<PullRequestLog>(QueryStrings.GetPullRequestLog,
                    new
                    {
                        CompanyCode = companyCode
                    })).SingleOrDefault();

                connection.Close();

                return result;
            }
        }

        public async Task UpdatePullRequestLog(PullRequestLog log)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteAsync(QueryStrings.UpdatePullRequestLog,
                    new
                    {
                        CompanyCode = log.CompanyCode,
                        IysCode = log.IysCode,
                        BrandCode = log.BrandCode,
                        AfterId = log.AfterId
                    });
                connection.Close();
            }
        }

        public async Task UpdateJustRequestDateOfPullRequestLog(PullRequestLog log)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteAsync(QueryStrings.UpdateJustRequestDateOfPullRequestLog,
                    new
                    {
                        CompanyCode = log.CompanyCode,
                        IysCode = log.IysCode,
                        BrandCode = log.BrandCode,
                    });
                connection.Close();
            }
        }



        public async Task InsertPullConsent(AddConsentRequest request)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteAsync(QueryStrings.InsertPullConsent,
                    new
                    {
                        CompanyCode = request.CompanyCode,
                        SalesforceId = request.SalesforceId,
                        IysCode = request.IysCode,
                        BrandCode = request.BrandCode,
                        ConsentDate = request.Consent.ConsentDate,
                        CreationDate = request.Consent.CreationDate,
                        Source = request.Consent.Source,
                        Recipient = request.Consent.Recipient,
                        RecipientType = request.Consent.RecipientType,
                        Status = request.Consent.Status,
                        Type = request.Consent.Type,
                        TransactionId = request.Consent.TransactionId
                    });
                connection.Close();
            }
        }

        public async Task<List<Consent>> GetPullConsentRequests(bool isProcessed, int rowCount)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = (await connection.QueryAsync<Consent>(string.Format(QueryStrings.GetPullConsentRequests, rowCount),
                    new
                    {
                        IsProcessed = isProcessed ? 1 : 0
                    })).ToList();

                connection.Close();

                return result;
            }

        }

        public async Task UpdateSfConsentResponse(SfConsentResult consentResult)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteAsync(QueryStrings.UpdateSfConsentRequest,
                    new
                    {
                        Id = consentResult.Id,
                        LogId = consentResult.LogId,
                        IsSuccess = consentResult.IsSuccess ? 1 : 0,
                        Error = consentResult.Error

                    });
                connection.Close();
            }
        }

        public async Task<List<Consent>> GetIYSConsentRequestErrors(DateTime? date = null)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();

                var result = (await connection.QueryAsync<Consent>(QueryStrings.GetIYSConsentRequestErrors,
                    new
                    {
                        CreateDate = (date ?? DateTime.Today)
                    })).ToList();

                connection.Close();

                return result;
            }
        }
    }
}
