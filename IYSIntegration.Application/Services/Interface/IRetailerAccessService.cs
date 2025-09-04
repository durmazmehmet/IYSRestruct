using IYSIntegration.Application.Base;
using IYSIntegration.Application.Request.RetailerAccess;
using IYSIntegration.Application.Response.RetailerAccess;

namespace IYSIntegration.Application.Services.Interface
{
    public interface IRetailerAccessService
    {
        Task<ResponseBase<AddRetailerAccessResult>> AddRetailerAccess(AddRetailerAccessRequest request);
        Task<ResponseBase<UpdateRetailerAccessResult>> UpdateRetailerAccess(UpdateRetailerAccessRequest request);
        Task<ResponseBase<DeleteAllRetailersAccessResult>> DeleteAllRetailersAccess(DeleteAllRetailersAccessRequest request);
        Task<ResponseBase<DeleteRetailerAccessResult>> DeleteRetailerAccess(DeleteRetailerAccessRequest request);
        Task<ResponseBase<QueryRetailerAccessResult>> QueryRetailerAccess(QueryRetailerAccessRequest request);
    }
}
