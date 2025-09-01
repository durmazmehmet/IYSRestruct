using IYSIntegration.Application.Base;

namespace IYSIntegration.Application.Request.RetailerAccess
{
    public class DeleteAllRetailersAccessRequest : Application.Base.ConsentParams
    {
        public RetailerRecipientAccess RetailerRecipientAccess { get; set; }
    }
}
