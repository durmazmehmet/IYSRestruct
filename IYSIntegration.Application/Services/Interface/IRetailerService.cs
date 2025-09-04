using IYSIntegration.Application.Base;
using IYSIntegration.Application.Request.Retailer;
using IYSIntegration.Application.Response.Retailer;

namespace IYSIntegration.Application.Services.Interface
{
    public interface IRetailerService
    {
        Task<ResponseBase<GetRetailerResponse>> GetRetailer(GetRetailerRequest request);
        Task<ResponseBase<GetAllRetailersResponse>> GetAllRetailers(GetAllRetailersRequest request);
        Task<ResponseBase<AddRetailerResponse>> AddRetailer(AddRetailerRequest request);
        Task<ResponseBase<DeleteRetailerResponse>> DeleteRetailer(DeleteRetailerRequest request);
    }
}
