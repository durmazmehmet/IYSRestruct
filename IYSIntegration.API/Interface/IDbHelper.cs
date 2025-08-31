using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Consent;
using IYSIntegration.Common.Response.Consent;
using RestSharp;

namespace IYSIntegration.API.Interface
{
    public interface IDbHelper
    {
        Task<int> InsertLog<TRequest>(IysRequest<TRequest> request);
        Task UpdateLog(RestResponse response, int id);
        Task<int> InsertConsentRequest(AddConsentRequest request);
        Task UpdateConsentResponse(ResponseBase<AddConsentResult> response);
        Task<ConsentResultLog> GetConsentRequest(long id);
        Task<int> GetMaxBatchId();
        Task<int> InsertConsentRequestWithBatch(AddConsentRequest request);
        Task InsertBatchConsentQuery(BatchConsentQuery request);
    }
}
