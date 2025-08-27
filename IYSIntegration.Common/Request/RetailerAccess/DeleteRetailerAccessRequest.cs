using IYSIntegration.Common.Base;

namespace IYSIntegration.Common.Request.RetailerAccess
{
    public class DeleteRetailerAccessRequest : Common.Base.ConsentParams
    {
        public RetailerRecipientAccess RetailerRecipientAccess { get; set; }
    }
}
