using IYSIntegration.Application.Services.Models.Base;

namespace IYSIntegration.Application.Services.Models.Request.RetailerAccess
{
    public class AddRetailerAccessRequest : ConsentParams
    {
        public RetailerRecipientAccess RetailerRecipientAccess { get; set; }
    }
}
