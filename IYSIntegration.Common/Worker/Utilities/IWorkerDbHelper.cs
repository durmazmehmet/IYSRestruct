using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Common.Response.Consent;
using IYSIntegration.Common.Worker.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IYSIntegration.Common.Worker
{
    public interface IWorkerDbHelper
    {
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
        Task<List<Consent>> GetIYSConsentRequestErrors();
    }
}
