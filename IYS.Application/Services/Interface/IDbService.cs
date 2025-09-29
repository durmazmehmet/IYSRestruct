using System;
using System.Collections.Generic;
using IYS.Application.Services.Models.Base;
using IYS.Application.Services.Models.Identity;
using IYS.Application.Services.Models.Request;
using IYS.Application.Services.Models.Response.Consent;
using IYS.Application.Services.Models.Response.Schedule;
using RestSharp;

namespace IYS.Application.Services.Interface
{
    public interface IDbService
    {
        Task<int> InsertLog<TRequest>(IysRequest<TRequest> request);
        Task UpdateLog(RestResponse response, int id);
        Task<int> InsertConsentRequest(AddConsentRequest request);        
        Task UpdateConsentResponseFromResponse(ResponseBase<AddConsentResult> response);
        Task<ConsentRequestLog?> GetConsentRequestById(long id);
        Task<ConsentResultLog> GetConsentRequest(string id);
        Task<List<ConsentRequestLog>> GetPendingConsents(int rowCount);
        Task UpdateConsentResponses(IEnumerable<ConsentResponseUpdate> responses);
        Task<PullRequestLog> GetPullRequestLog(string companyCode);
        Task UpdatePullRequestLog(PullRequestLog log);
        Task UpdateJustRequestDateOfPullRequestLog(PullRequestLog log);
        Task InsertPullConsent(AddConsentRequest request);
        Task<List<Consent>> GetPullConsentRequests(bool isProcessed, int rowCount);
        Task UpdateSfConsentResponse(SfConsentResult consentResult);
        Task<List<ConsentErrorModel>> GetIYSConsentRequestErrors(DateTime? date = null);
        Task<T> UpdateLogFromResponseBase<T>(ResponseBase<T> response, int id);
        Task<List<PullConsentSummary>> GetPullConsentsAsync(DateTime startDate, string recipientType, IEnumerable<string> companyCodes);
        Task InsertTokenLogAsync(TokenLogEntry tokenLogEntry);
        Task<DateTime?> GetLastTokenCreateDateUtcAsync(string companyCode);
        Task<int> UpdateTokenResponseLog(TokenResponseLog log);
        Task<TokenResponseLog?> GetTokenResponseLog(string cacheKey);
        Task SetTokenHaltUntilAsync(string cacheKey, DateTime? haltUntilUtc);
        Task<DateTime?> GetTokenHaltUntilAsync(string cacheKey);
    }
}
