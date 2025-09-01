using IYSIntegration.Common.Base;

namespace IYSIntegration.Application.Interface
{
    public interface IRestClientService
    {
        Task<ResponseBase<TResponse>> Execute<TResponse, TBody>(Common.Base.IysRequest<TBody> IysRequest);
    }
}
