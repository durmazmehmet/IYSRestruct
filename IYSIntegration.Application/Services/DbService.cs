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
using System.Data.SqlClient;
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

        public async Task<List<string>> GetExistingConsentRecipients(string companyCode, string? type, IEnumerable<string> recipients)
        {
            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                await connection.OpenAsync();

                var result = (await connection.QueryAsync<string>(
                    QueryStrings.GetExistingConsentRecipients,
                    new
                    {
                        CompanyCode = companyCode,
                        Type = string.IsNullOrWhiteSpace(type) ? null : type.Trim(),
                        Recipients = recipients
                    })).ToList();

                await connection.CloseAsync();

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
                        IsSuccess = response.IsSuccessful() ? 1 : 0,
                        response.Data?.TransactionId,
                        response.Data?.CreationDate
                    }); ;
                connection.Close();
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
                var result = (await connection.QueryAsync<ConsentRequestLog>(string.Format(QueryStrings.GetPendingConsentsWithoutPull, rowCount))).ToList();
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
                                    Id = response.Id,
                                    LogId = response.LogId,
                                    IsSuccess = response.IsSuccess ? 1 : 0,
                                    TransactionId = response.TransactionId,
                                    CreationDate = response.CreationDate,
                                    BatchError = response.BatchError,
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

        public async Task UpdatePullConsentStatuses(string companyCode, string recipientType, string type, IEnumerable<string> recipients, string status)
        {
            if (string.IsNullOrWhiteSpace(companyCode)
                || string.IsNullOrWhiteSpace(recipientType)
                || string.IsNullOrWhiteSpace(type)
                || string.IsNullOrWhiteSpace(status))
            {
                return;
            }

            var recipientList = recipients?
                .Where(recipient => !string.IsNullOrWhiteSpace(recipient))
                .Select(recipient => recipient.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (recipientList == null || recipientList.Count == 0)
            {
                return;
            }

            using (var connection = new SqlConnection(_configuration.GetValue<string>("ConnectionStrings:SfdcMasterData")))
            {
                await connection.OpenAsync();

                await connection.ExecuteAsync(QueryStrings.UpdatePullConsentStatuses,
                    new
                    {
                        CompanyCode = companyCode,
                        RecipientType = recipientType,
                        Type = type,
                        Status = status.ToUpperInvariant(),
                        Recipients = recipientList
                    });

                connection.Close();
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
