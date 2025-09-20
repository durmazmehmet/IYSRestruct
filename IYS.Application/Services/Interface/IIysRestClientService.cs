using IYS.Application.Services.Models.Base;

namespace IYS.Application.Services.Interface
{
    public interface IIysRestClientService
    {
        Task<ResponseBase<TResponse>> Execute<TResponse, TBody>(IysRequest<TBody> IysRequest);
    }
}
