using Dapper;
using IYSIntegration.Application.Services.Constants;
using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request.Consent;
using IYSIntegration.Application.Services.Models.Response.Consent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
namespace IYSIntegration.Application.Services
{
    public class DbService : IDbService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DbService> _logger;

        public DbService(IConfiguration configuration, ILogger<DbService> loggerServiceBase)
        {
            _configuration = configuration;
            _logger = loggerServiceBase;
        }

        internal sealed record DuplicateCleanupCandidate(
            string CompanyCode,
            string Recipient,
            string RecipientType,
            string Type,
            string Status);

        internal static IReadOnlyList<DuplicateCleanupCandidate> BuildDuplicateCleanupCandidates(IEnumerable<Consent> consents)
        {
            if (consents == null)
            {
                return Array.Empty<DuplicateCleanupCandidate>();
            }

            return consents
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.CompanyCode) && !string.IsNullOrWhiteSpace(c.Recipient))
                .Select(c => new DuplicateCleanupCandidate(
                    c.CompanyCode!,
                    c.Recipient!,
                    c.RecipientType ?? string.Empty,
                    c.Type ?? string.Empty,
                    c.Status ?? string.Empty))
                .Distinct()
                .ToList();
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
            if (string.IsNullOrWhiteSpace(request.CompanyCode)
                && !string.IsNullOrWhiteSpace(request.CompanyName))
            {
                request.CompanyCode = request.CompanyName;
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

        public async Task<bool> CheckConsentRequest(AddConsentRequest request)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteScalarAsync<int>(QueryStrings.CheckConsentRequest,
                    new
                    {
                        request.Consent.Status,
                        request.Consent.Recipient
                    });
                connection.Close();

                return result == 1;
            }
        }

        public async Task<bool> PullConsentExists(string companyCode, string recipient, string? type = null)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteScalarAsync<int>(QueryStrings.CheckPullConsent,
                    new
                    {
                        CompanyCode = companyCode,
                        Recipient = recipient,
                        Type = string.IsNullOrWhiteSpace(type) ? null : type
                    });
                connection.Close();

                return result == 1;
            }
        }

        public async Task<bool> SuccessfulConsentRequestExists(string companyCode, string recipient, string? type = null)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                await connection.OpenAsync();

                var result = await connection.ExecuteScalarAsync<int>(QueryStrings.CheckSuccessfulConsentRequest,
                    new
                    {
                        CompanyCode = companyCode,
                        Recipient = recipient,
                        Type = string.IsNullOrWhiteSpace(type) ? null : type
                    });

                await connection.CloseAsync();

                return result == 1;
            }
        }

        public async Task<DateTime?> GetLastConsentDate(string companyCode, string recipient)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteScalarAsync<DateTime?>(QueryStrings.GetLastConsentDate,
                    new
                    {
                        CompanyCode = companyCode,
                        Recipient = recipient
                    });
                connection.Close();

                return result;
            }
        }

        public async Task<List<Consent>> GetLastConsents(string companyCode, IEnumerable<string> recipients)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = (await connection.QueryAsync<Consent>(
    QueryStrings.GetLastConsents,
    new { CompanyCode = companyCode, Recipients = recipients } // IEnumerable<string>
)).ToList();
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
            if (string.IsNullOrWhiteSpace(request.CompanyCode)
                && !string.IsNullOrWhiteSpace(request.CompanyName))
            {
                request.CompanyCode = request.CompanyName;
            }

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

        public async Task<List<ConsentRequestLog>> GetPendingConsentsWithoutPull(int rowCount)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = (await connection.QueryAsync<ConsentRequestLog>(string.Format(QueryStrings.GetPendingConsentsWithoutPull, rowCount))).ToList();
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
            if (response == null)
            {
                return;
            }

            await UpdateConsentResponses(new[] { response });
        }

        public async Task UpdateConsentResponses(IEnumerable<ResponseBase<AddConsentResult>> responses)
        {
            if (responses == null)
            {
                return;
            }

            var responseList = responses.Where(r => r != null).ToList();

            if (responseList.Count == 0)
            {
                return;
            }

            var overdueErrors = new HashSet<string> { "H174", "H175", "H178" };

            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var response in responseList)
                        {
                            var errorCodeList = response.OriginalError?.Errors?
                                .Select(x => x.Code)
                                .Where(code => !string.IsNullOrWhiteSpace(code))
                                .ToList() ?? new List<string>();

                            var isConsentOverdue = response.OriginalError?.Errors?.Any(x => !string.IsNullOrWhiteSpace(x.Code) && overdueErrors.Contains(x.Code)) ?? false;

                            var errors = errorCodeList.Count > 0 ? string.Join(",", errorCodeList) : "Mevcut olmayan";

                            if (response.HttpStatusCode == 200)
                            {
                                _logger.LogInformation($"SingleConsentWorker ID {response.Id} ve {response.HttpStatusCode} statu olarak alındı");
                            }
                            else if (isConsentOverdue)
                            {
                                _logger.LogWarning($"SingleConsentWorker ID {response.Id} ve IYS geciken/mükerrer {errors} olarak alındı");
                            }
                            else
                            {
                                _logger.LogError($"SingleConsentWorker ID {response.Id} ve {response.HttpStatusCode} statu kodu ve {errors} IYS hataları ile alınamadı");
                            }

                            await connection.ExecuteAsync(QueryStrings.UpdateConsentRequest,
                                new
                                {
                                    Id = response.Id,
                                    LogId = response.LogId,
                                    IsSuccess = isConsentOverdue ? 0 : response.IsSuccessful() ? 1 : 0,
                                    TransactionId = response.Data?.TransactionId,
                                    CreationDate = response.Data?.CreationDate,
                                    BatchError = response.OriginalError == null ? null : JsonConvert.SerializeObject(response.OriginalError,
                                        Formatting.None, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }),
                                    IsOverdue = isConsentOverdue ? 1 : 0
                                },
                                transaction);
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
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

        public async Task<int> MarkConsentsOverdue(int maxAgeInDays)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var affected = await connection.ExecuteAsync(QueryStrings.MarkConsentsOverdue, new { MaxAgeInDays = maxAgeInDays });
                connection.Close();

                return affected;
            }
        }

        public async Task<int> MarkDuplicateConsentsOverdue()
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var affected = await connection.ExecuteAsync(QueryStrings.MarkDuplicateConsentsOverdue);
                connection.Close();

                return affected;
            }
        }

        public async Task<int> MarkDuplicateConsentsOverdueForConsents(IEnumerable<Consent> consents)
        {
            var candidates = BuildDuplicateCleanupCandidates(consents);

            if (candidates.Count == 0)
            {
                return 0;
            }

            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                await connection.OpenAsync();

                var totalAffected = 0;

                foreach (var candidate in candidates)
                {
                    totalAffected += await connection.ExecuteAsync(
                        QueryStrings.MarkDuplicateConsentsOverdueForRecipient,
                        new
                        {
                            candidate.CompanyCode,
                            candidate.Recipient,
                            RecipientType = candidate.RecipientType,
                            candidate.Type,
                            candidate.Status
                        });
                }

                await connection.CloseAsync();

                return totalAffected;
            }
        }

        public async Task MarkConsentsAsNotPulled(IEnumerable<long> consentIds)
        {
            if (consentIds == null)
            {
                return;
            }

            var ids = consentIds
                .Where(id => id > 0)
                .Distinct()
                .ToArray();

            if (ids.Length == 0)
            {
                return;
            }

            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                await connection.OpenAsync();

                await connection.ExecuteAsync(QueryStrings.MarkConsentsAsNotPulled, new { Ids = ids });

                await connection.CloseAsync();
            }
        }

        public async Task MarkConsentsAsPulled(IEnumerable<long> consentIds)
        {
            if (consentIds == null)
            {
                return;
            }

            var ids = consentIds
                .Where(id => id > 0)
                .Distinct()
                .ToArray();

            if (ids.Length == 0)
            {
                return;
            }

            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                await connection.OpenAsync();

                await connection.ExecuteAsync(QueryStrings.MarkConsentsAsPulled, new { Ids = ids });

                await connection.CloseAsync();
            }
        }
    }
}
