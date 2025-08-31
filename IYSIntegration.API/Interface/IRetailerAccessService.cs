using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.RetailerAccess;
using IYSIntegration.Common.Response.RetailerAccess;

namespace IYSIntegration.API.Interface
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
