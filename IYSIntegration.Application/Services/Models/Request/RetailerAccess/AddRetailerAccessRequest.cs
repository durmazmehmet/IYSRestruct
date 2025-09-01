using IYSIntegration.Application.Base;

namespace IYSIntegration.Application.Request.RetailerAccess
{
    public class AddRetailerAccessRequest : Application.Base.ConsentParams
    {
        public RetailerRecipientAccess RetailerRecipientAccess { get; set; }
    }
}
