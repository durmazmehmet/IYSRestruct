using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Base;
using IYSIntegration.Application.Request.Consent;
using IYSIntegration.Application.Response.Consent;
using RestSharp;
using System;

namespace IYSIntegration.Application.Services.Interface
{
    public interface IDbService
    {
        Task<int> InsertLog<TRequest>(IysRequest<TRequest> request);
        Task UpdateLog(RestResponse response, int id);
        Task<int> InsertConsentRequest(AddConsentRequest request);
        Task UpdateConsentResponseFromCommon(ResponseBase<AddConsentResult> response);
        Task<ConsentResultLog> GetConsentRequest(long id);
        Task<int> GetMaxBatchId();
        Task<int> InsertConsentRequestWithBatch(AddConsentRequest request);
        Task InsertBatchConsentQuery(BatchConsentQuery request);
        Task<List<ConsentRequestLog>> GetConsentRequests(bool isProcessed, int rowCount);
        Task UpdateConsentResponse(ResponseBase<AddConsentResult> response);
        Task UpdateBatchId(string companyCode, int batchSize);
        Task<List<BatchSummary>> GetBatchSummary(int batchCount);
        Task<List<ConsentRequestLog>> GeBatchConsentRequests(int batchId);
        Task UpdateBatchConsentRequests(BatchConsentQuery query);
        Task<List<BatchConsentQuery>> GetUnprocessedMultipleConsenBatches(int batchCount);
        Task UpdateMultipleConsentQueryDate(int batchId, long logId);
        Task UpdateMultipleConsentItem(BatchItemResult batchItemResult);
        Task ReorderBatch(int oldBatchId);
        Task<PullRequestLog> GetPullRequestLog(string companyCode);
        Task UpdatePullRequestLog(PullRequestLog log);
        Task UpdateJustRequestDateOfPullRequestLog(PullRequestLog log);
        Task InsertPullConsent(AddConsentRequest request);
        Task<List<Consent>> GetPullConsentRequests(bool isProcessed, int rowCount);
        Task UpdateSfConsentResponse(SfConsentResult consentResult);
        Task<List<Consent>> GetIYSConsentRequestErrors(DateTime? date = null);
        Task<T> UpdateLogFromResponseBase<T>(ResponseBase<T> response, int id);
    }
}
