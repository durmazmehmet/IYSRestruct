using IYSIntegration.Application.Services.Models.Base;

namespace IYSIntegration.Application.Services.Models.Request
{
    public class DeleteRetailerAccessRequest : ConsentParams
    {
        public RetailerRecipientAccess RetailerRecipientAccess { get; set; }
    }
}
