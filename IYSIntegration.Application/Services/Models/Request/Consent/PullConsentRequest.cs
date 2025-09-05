using IYSIntegration.Application.Services.Models.Base;

namespace IYSIntegration.Application.Services.Models.Request.Consent
{
    public class PullConsentRequest : ConsentParams
    {
        public string CompanyCode { get; set; }
        public string? After { get; set; }
        public int? Limit { get; set; }
        public string Source { get; set; }
    }
}
