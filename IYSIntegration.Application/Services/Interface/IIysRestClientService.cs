using IYSIntegration.Application.Base;

namespace IYSIntegration.Application.Services.Interface
{
    public interface IIysRestClientService
    {
        Task<ResponseBase<TResponse>> Execute<TResponse, TBody>(IysRequest<TBody> IysRequest);
    }
}
