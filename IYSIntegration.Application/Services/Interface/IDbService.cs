using System.Collections.Generic;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Identity;
using IYSIntegration.Application.Services.Models.Request;
using IYSIntegration.Application.Services.Models.Response.Consent;
using IYSIntegration.Application.Services.Models.Response.Schedule;
using RestSharp;

namespace IYSIntegration.Application.Services.Interface
{
    public interface IDbService
    {
        Task<int> InsertLog<TRequest>(IysRequest<TRequest> request);
        Task UpdateLog(RestResponse response, int id);
        Task<int> InsertConsentRequest(AddConsentRequest request);
        Task<bool> PullConsentExists(string companyCode, string recipient, string? type = null);
        Task<bool> SuccessfulConsentRequestExists(string companyCode, string recipient, string? type = null);
        Task<List<string>> GetExistingConsentRecipients(string companyCode, string? type, IEnumerable<string> recipients);
        Task UpdateConsentResponseFromResponse(ResponseBase<AddConsentResult> response);
        Task<ConsentResultLog> GetConsentRequest(string id);
        Task<List<ConsentRequestLog>> GetPendingConsents(int rowCount);
        Task UpdateConsentResponses(IEnumerable<ConsentResponseUpdate> responses);
        Task<PullRequestLog> GetPullRequestLog(string companyCode);
        Task UpdatePullRequestLog(PullRequestLog log);
        Task UpdateJustRequestDateOfPullRequestLog(PullRequestLog log);
        Task InsertPullConsent(AddConsentRequest request);
        Task<List<Consent>> GetPullConsentRequests(bool isProcessed, int rowCount);
        Task UpdatePullConsentStatuses(string companyCode, string recipientType, string type, IEnumerable<string> recipients, string status);
        Task UpdateSfConsentResponse(SfConsentResult consentResult);
        Task<List<Consent>> GetIYSConsentRequestErrors(DateTime? date = null);
        Task<T> UpdateLogFromResponseBase<T>(ResponseBase<T> response, int id);
        Task<List<PullConsentSummary>> GetPullConsentsAsync(DateTime startDate, string recipientType, IEnumerable<string> companyCodes);
        Task InsertTokenLogAsync(TokenLogEntry tokenLogEntry);
    }
}
