using IYSIntegration.Application.Services.Models.Base;

namespace IYSIntegration.Application.Services.Interface
{
    public interface ISimpleRestClient
    {
        Task<ResponseBase<T>> GetAsync<T>(string path, IDictionary<string, string?>? query = null, CancellationToken ct = default);
        Task<ResponseBase<T>> PostFormAsync<T>(string path, IDictionary<string, string> form, CancellationToken ct = default);
        Task<ResponseBase<TResp>> PostJsonAsync<TReq, TResp>(string path, TReq body, CancellationToken ct = default);
    }
}