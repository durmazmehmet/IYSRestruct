using Dapper;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Common.Response.Consent;
using IYSIntegration.Scheduled.Constants;
using IYSIntegration.Scheduled.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace IYSIntegration.Scheduled.Utilities
{
    public class DbHelper : IDbHelper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DbHelper> _logger;

        public DbHelper(IConfiguration configuration, ILogger<DbHelper> loggerServiceBase)
        {
            _configuration = configuration;
            _logger = loggerServiceBase;
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

        public async Task<List<Consent>> GetIYSConsentRequestErrors()
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();

                var result = (await connection.QueryAsync<Consent>(QueryStrings.GetIYSConsentRequestErrors,
                    new
                    {
                        CreateDate = DateTime.Today
                    })).ToList();

                connection.Close();

                return result;
            }
        }
    }
}
