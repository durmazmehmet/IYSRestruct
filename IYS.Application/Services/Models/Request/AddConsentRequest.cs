using IYS.Application.Services.Models.Base;

namespace IYS.Application.Services.Models.Request
{
    public class AddConsentRequest : ConsentParams
    {
        public Consent Consent { get; set; }
        public string? SalesforceId { get; set; }
        public string? CompanyCode { get; set; }
        public string? CompanyName { get; set; }
    }
}
