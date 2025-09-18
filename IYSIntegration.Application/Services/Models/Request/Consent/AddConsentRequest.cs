using IYSIntegration.Application.Services.Models.Base;

namespace IYSIntegration.Application.Services.Models.Request.Consent
{
    public class AddConsentRequest : ConsentParams
    {
        public Base.Consent Consent { get; set; }
        public bool WithoutLogging { get; set; }
        public string? SalesforceId { get; set; }
        public string? CompanyCode { get; set; }
        public string? CompanyName { get; set; }
    }
}
