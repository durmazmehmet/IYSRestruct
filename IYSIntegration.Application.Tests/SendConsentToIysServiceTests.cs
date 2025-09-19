using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IYSIntegration.Application.Services;
using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request;
using IYSIntegration.Application.Services.Models.Response.Consent;
using IYSIntegration.Application.Services.Models.Response.Schedule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IYSIntegration.Application.Tests
{
    public class SendConsentToIysServiceTests
    {
        [Fact]
        public async Task RunAsync_Skips_WhenStatusAlreadyApprovedInIys()
        {
            var db = new FakeDbService
            {
                PendingConsents =
                {
                    new ConsentRequestLog
                    {
                        Id = 1,
                        CompanyCode = "C1",
                        IysCode = 123,
                        BrandCode = 456,
                        Recipient = "user@example.com",
                        RecipientType = "BIREYSEL",
                        Type = "EPOSTA",
                        Status = "ON",
                        ConsentDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                    }
                },
            };

            var proxy = new FakeIysProxy();
            proxy.ApprovedRecipients.Add("user@example.com");
            var service = CreateService(db, proxy);

            await service.RunAsync(10);

            Assert.Equal(0, proxy.AddConsentCallCount);
            Assert.Equal(1, proxy.QueryMultipleCallCount);
            var update = Assert.Single(db.PersistedUpdates);
            Assert.True(update.IsOverdue);
            Assert.False(update.IsSuccess);
            Assert.Contains("SKIP_ALREADY_APPROVED", update.BatchError);
        }

        [Fact]
        public async Task RunAsync_Skips_RetWhenMissingFromIys()
        {
            var db = new FakeDbService
            {
                PendingConsents =
                {
                    new ConsentRequestLog
                    {
                        Id = 2,
                        CompanyCode = "C1",
                        IysCode = 123,
                        BrandCode = 456,
                        Recipient = "first-ret@example.com",
                        RecipientType = "BIREYSEL",
                        Type = "SMS",
                        Status = "RET",
                        ConsentDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                    }
                }
            };

            var proxy = new FakeIysProxy();
            var service = CreateService(db, proxy);

            await service.RunAsync(10);

            Assert.Equal(0, proxy.AddConsentCallCount);
            Assert.Equal(1, proxy.QueryMultipleCallCount);
            var update = Assert.Single(db.PersistedUpdates);
            Assert.True(update.IsOverdue);
            Assert.Contains("SKIP_RET_NOT_PRESENT", update.BatchError);
        }

        [Fact]
        public async Task RunAsync_Sends_WhenApprovalMissingFromIys()
        {
            var db = new FakeDbService
            {
                PendingConsents =
                {
                    new ConsentRequestLog
                    {
                        Id = 3,
                        CompanyCode = "C1",
                        IysCode = 123,
                        BrandCode = 456,
                        Recipient = "old@example.com",
                        RecipientType = "BIREYSEL",
                        Type = "ARAMA",
                        Status = "ON",
                        ConsentDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                    }
                }
            };

            var proxy = new FakeIysProxy();
            var service = CreateService(db, proxy);

            await service.RunAsync(10);

            Assert.Equal(1, proxy.AddConsentCallCount);
            Assert.Equal(1, proxy.QueryMultipleCallCount);
            Assert.Single(db.PersistedUpdates);
            Assert.DoesNotContain(db.PersistedUpdates, update => update.IsOverdue);
        }

        private static SendConsentToIysService CreateService(FakeDbService dbService, FakeIysProxy proxy)
        {
            var logger = NullLogger<SendConsentToIysService>.Instance;
            var helper = new FakeIysHelper();
            IConfiguration configuration = new ConfigurationBuilder().Build();
            return new SendConsentToIysService(logger, dbService, helper, proxy, configuration);
        }

        private sealed class FakeIysProxy : IIysProxy
        {
            public HashSet<string> ApprovedRecipients { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public int AddConsentCallCount { get; private set; }

            public int QueryMultipleCallCount { get; private set; }

            public Task<ResponseBase<T>> GetAsync<T>(string path, IDictionary<string, string?>? query = null, CancellationToken ct = default)
            {
                throw new NotImplementedException();
            }

            public Task<ResponseBase<T>> PostFormAsync<T>(string path, IDictionary<string, string> form, CancellationToken ct = default)
            {
                throw new NotImplementedException();
            }

            public Task<ResponseBase<TResp>> PostJsonAsync<TReq, TResp>(string path, TReq body, CancellationToken ct = default)
            {
                if (path.Contains("queryMultipleConsent", StringComparison.OrdinalIgnoreCase))
                {
                    QueryMultipleCallCount++;
                    if (typeof(TResp) != typeof(MultipleQueryConsentResult))
                    {
                        throw new InvalidOperationException("Unexpected response type for queryMultipleConsent.");
                    }

                    var result = new MultipleQueryConsentResult
                    {
                        List = ApprovedRecipients.ToList()
                    };

                    return Task.FromResult(new ResponseBase<TResp>
                    {
                        HttpStatusCode = 200,
                        Status = ServiceResponseStatuses.Success,
                        Data = (TResp)(object)result
                    });
                }

                if (path.Contains("addConsent", StringComparison.OrdinalIgnoreCase))
                {
                    AddConsentCallCount++;

                    return Task.FromResult(new ResponseBase<TResp>
                    {
                        HttpStatusCode = 200,
                        Status = ServiceResponseStatuses.Success,
                        Data = typeof(TResp) == typeof(AddConsentResult)
                            ? (TResp)(object)new AddConsentResult()
                            : default!
                    });
                }

                throw new InvalidOperationException($"Unexpected path: {path}");
            }
        }

        private sealed class FakeIysHelper : IIysHelper
        {
            public ConsentParams GetIysCode(string companyCode)
                => new ConsentParams { BrandCode = 0, CompanyCode = companyCode, IysCode = 0 };

            public string? GetCompanyCode(int code) => null;

            public List<string> GetAllCompanyCodes() => new List<string>();

            public string? ResolveCompanyCode(string? companyCode, int iysCode) => companyCode;

            public string BuildAddConsentErrorMessage(ResponseBase<AddConsentResult> addResponse) => string.Empty;

            public bool IsForceSendEnabled() => false;
        }

        private sealed class FakeDbService : IDbService
        {
            public List<ConsentRequestLog> PendingConsents { get; set; } = new List<ConsentRequestLog>();

            public List<ConsentResponseUpdate> PersistedUpdates { get; } = new List<ConsentResponseUpdate>();

            public Task<List<ConsentRequestLog>> GetPendingConsents(int rowCount)
                => Task.FromResult(PendingConsents);

            public Task UpdateConsentResponses(IEnumerable<ConsentResponseUpdate> responses)
            {
                if (responses != null)
                {
                    PersistedUpdates.AddRange(responses);
                }

                return Task.CompletedTask;
            }

            public Task<int> InsertLog<TRequest>(IysRequest<TRequest> request) => throw new NotImplementedException();

            public Task UpdateLog(RestSharp.RestResponse response, int id) => throw new NotImplementedException();

            public Task<int> InsertConsentRequest(AddConsentRequest request) => throw new NotImplementedException();

            public Task<bool> PullConsentExists(string companyCode, string recipient, string? type = null) => throw new NotImplementedException();

            public Task<bool> SuccessfulConsentRequestExists(string companyCode, string recipient, string? type = null) => throw new NotImplementedException();

            public Task<List<string>> GetExistingConsentRecipients(string companyCode, string? type, IEnumerable<string> recipients) => throw new NotImplementedException();

            public Task UpdateConsentResponseFromResponse(ResponseBase<AddConsentResult> response) => throw new NotImplementedException();

            public Task<ConsentResultLog> GetConsentRequest(string id) => throw new NotImplementedException();

            public Task<PullRequestLog> GetPullRequestLog(string companyCode) => throw new NotImplementedException();

            public Task UpdatePullRequestLog(PullRequestLog log) => throw new NotImplementedException();

            public Task UpdateJustRequestDateOfPullRequestLog(PullRequestLog log) => throw new NotImplementedException();

            public Task InsertPullConsent(AddConsentRequest request) => throw new NotImplementedException();

            public Task<List<Consent>> GetPullConsentRequests(bool isProcessed, int rowCount) => throw new NotImplementedException();

            public Task UpdatePullConsentStatuses(string companyCode, string recipientType, string type, IEnumerable<string> recipients, string status) => throw new NotImplementedException();

            public Task UpdateSfConsentResponse(SfConsentResult consentResult) => throw new NotImplementedException();

            public Task<List<Consent>> GetIYSConsentRequestErrors(DateTime? date = null) => throw new NotImplementedException();

            public Task<T> UpdateLogFromResponseBase<T>(ResponseBase<T> response, int id) => throw new NotImplementedException();

            public Task<List<PullConsentSummary>> GetPullConsentsAsync(DateTime startDate, string recipientType, IEnumerable<string> companyCodes) => throw new NotImplementedException();

            public Task InsertTokenLogAsync(TokenLogEntry tokenLogEntry) => throw new NotImplementedException();
        }
    }
}
