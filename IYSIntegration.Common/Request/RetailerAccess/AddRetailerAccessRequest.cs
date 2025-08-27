using IYSIntegration.Common.Base;

namespace IYSIntegration.Common.Request.RetailerAccess
{
    public class AddRetailerAccessRequest : Common.Base.ConsentParams
    {
        public RetailerRecipientAccess RetailerRecipientAccess { get; set; }
    }
}
