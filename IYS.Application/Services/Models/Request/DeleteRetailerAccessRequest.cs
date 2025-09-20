using IYS.Application.Services.Models.Base;

namespace IYS.Application.Services.Models.Request
{
    public class DeleteRetailerAccessRequest : ConsentParams
    {
        public RetailerRecipientAccess RetailerRecipientAccess { get; set; }
    }
}
