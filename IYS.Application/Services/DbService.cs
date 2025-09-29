using Dapper;
using IYS.Application.Services.Constants;
using IYS.Application.Services.Interface;
using IYS.Application.Services.Models.Base;
using IYS.Application.Services.Models.Identity;
using IYS.Application.Services.Models.Request;
using IYS.Application.Services.Models.Response.Consent;
using IYS.Application.Services.Models.Response.Schedule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
namespace IYS.Application.Services
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

        public async Task InsertTokenLogAsync(TokenLogEntry tokenLogEntry)
        {
            if (tokenLogEntry is null)
            {
                throw new ArgumentNullException(nameof(tokenLogEntry));
            }

            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                await connection.OpenAsync();

                await connection.ExecuteAsync(QueryStrings.InsertTokenLog, new
                {
                    tokenLogEntry.CompanyCode,
                    tokenLogEntry.AccessTokenMasked,
                    tokenLogEntry.RefreshTokenMasked,
                    tokenLogEntry.TokenUpdateDateUtc,
                    tokenLogEntry.Operation,
                    tokenLogEntry.ServerIdentifier
                });

                await connection.CloseAsync();
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

        public async Task<List<PullConsentSummary>> GetPullConsentsAsync(DateTime startDate, string recipientType, IEnumerable<string> companyCodes)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                await connection.OpenAsync();

                var result = (await connection.QueryAsync<PullConsentSummary>(
                    QueryStrings.GetPullConsentsByFilter,
                    new
                    {
                        StartDate = startDate,
                        RecipientType = recipientType,
                        CompanyCodes = companyCodes
                    })).ToList();

                await connection.CloseAsync();

                return result;
            }
        }

        public async Task<int> InsertConsentRequest(AddConsentRequest request)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
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

        public async Task UpdateConsentResponseFromResponse(ResponseBase<AddConsentResult> response)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.ExecuteAsync(QueryStrings.UpdateConsentRequestFromCommon,
                    new
                    {
                        response.Id,
                        response.LogId,
                        BatchId = (long?)null,
                        IsSuccess = response.IsSuccessful() ? 1 : 0,
                        response.Data?.TransactionId,
                        response.Data?.CreationDate
                    }); ;
                connection.Close();
            }
        }

        public async Task<ConsentRequestLog?> GetConsentRequestById(long id)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                await connection.OpenAsync();

                var result = await connection.QuerySingleOrDefaultAsync<ConsentRequestLog>(
                    QueryStrings.GetConsentRequestById,
                    new
                    {
                        Id = id
                    });

                await connection.CloseAsync();

                return result;
            }
        }

        public async Task<ConsentResultLog> GetConsentRequest(string recipient)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = (await connection.QueryAsync<ConsentResultLog>(QueryStrings.QueryConsentRequest,
                    new
                    {
                        Recipient = recipient
                    })).SingleOrDefault();

                connection.Close();

                return result;
            }
        }

        public async Task<List<ConsentRequestLog>> GetPendingConsents(int rowCount)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = (await connection.QueryAsync<ConsentRequestLog>(string.Format(QueryStrings.GetPendingConsents, rowCount))).ToList();
                connection.Close();

                return result;
            }

        }

        public async Task UpdateConsentResponses(IEnumerable<ConsentResponseUpdate> responses)
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

            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var response in responseList)
                        {
                            await connection.ExecuteAsync(QueryStrings.UpdateConsentRequest,
                                new
                                {
                                    response.Id,
                                    response.LogId,
                                    response.BatchId,
                                    IsSuccess = response.IsSuccess ? 1 : 0,
                                    response.TransactionId,
                                    response.CreationDate,
                                    response.BatchError,
                                    IsOverdue = response.IsOverdue ? 1 : 0,
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

        public async Task<PullRequestLog> GetPullRequestLog(string companyCode)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = (await connection.QueryAsync<PullRequestLog>(QueryStrings.GetPullRequestLog,
                    new
                    {
                        IysCode = companyCode
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
                        log.CompanyCode,
                        log.IysCode,
                        log.BrandCode,
                        log.AfterId
                    });
                connection.Close();
            }
        }

        public async Task<int> UpdateTokenResponseLog(TokenResponseLog log)
        {
            await using var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData"));
            await connection.OpenAsync();

            var result = await connection.ExecuteAsync(QueryStrings.UpdateTokenResponseLog,
                new
                {
                    log.IysCode,
                    log.TokenResponse
                });

            await connection.CloseAsync();

            return result;
        }

        public async Task<string> GetTokenResponseLog(string IysCode)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                connection.Open();
                var result = await connection.QueryAsync<string>(QueryStrings.GetTokenResponseLog,
                    new
                    {
                        IysCode
                    });
                connection.Close();

                return result.FirstOrDefault() ?? string.Empty;
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
                        log.CompanyCode,
                        log.IysCode,
                        log.BrandCode,
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
                        request.CompanyCode,
                        request.SalesforceId,
                        request.IysCode,
                        request.BrandCode,
                        request.Consent.ConsentDate,
                        request.Consent.CreationDate,
                        request.Consent.Source,
                        request.Consent.Recipient,
                        request.Consent.RecipientType,
                        request.Consent.Status,
                        request.Consent.Type,
                        request.Consent.TransactionId
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
                        consentResult.Id,
                        consentResult.LogId,
                        IsSuccess = consentResult.IsSuccess ? 1 : 0,
                        consentResult.Error

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
                        CreateDate = date ?? DateTime.Today
                    })).ToList();

                connection.Close();

                return result;
            }
        }

    }
}
