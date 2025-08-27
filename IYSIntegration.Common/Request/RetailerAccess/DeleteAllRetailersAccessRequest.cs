using IYSIntegration.Common.Base;

namespace IYSIntegration.Common.Request.RetailerAccess
{
    public class DeleteAllRetailersAccessRequest : Common.Base.ConsentParams
    {
        public RetailerRecipientAccess RetailerRecipientAccess { get; set; }
    }
}
