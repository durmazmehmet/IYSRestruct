using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Retailer;
using IYSIntegration.Common.Response.Retailer;

namespace IYSIntegration.Application.Interface
{
    public interface IRetailerService
    {
        Task<ResponseBase<GetRetailerResponse>> GetRetailer(GetRetailerRequest request);
        Task<ResponseBase<GetAllRetailersResponse>> GetAllRetailers(GetAllRetailersRequest request);
        Task<ResponseBase<AddRetailerResponse>> AddRetailer(AddRetailerRequest request);
        Task<ResponseBase<DeleteRetailerResponse>> DeleteRetailer(DeleteRetailerRequest request);
    }
}
