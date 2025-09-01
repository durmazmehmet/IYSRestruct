using IYSIntegration.Application.Base;

namespace IYSIntegration.Application.Request.RetailerAccess
{
    public class DeleteRetailerAccessRequest : Application.Base.ConsentParams
    {
        public RetailerRecipientAccess RetailerRecipientAccess { get; set; }
    }
}
