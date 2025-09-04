using IYSIntegration.Application.Base;

namespace IYSIntegration.Application.Services.Interface
{
    public interface IRestClientService
    {
        Task<ResponseBase<TResponse>> Execute<TResponse, TBody>(IysRequest<TBody> IysRequest);
    }
}
