using IYSIntegration.Common.Base;

namespace IYSIntegration.Application.Interface
{
    public interface IRestClientHelper
    {
        Task<ResponseBase<TResponse>> Execute<TResponse, TBody>(Common.Base.IysRequest<TBody> IysRequest);
    }
}
