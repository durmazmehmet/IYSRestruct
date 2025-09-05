using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Request.Consent
{
    public class SfConsentBase
    {
        [JsonProperty("companyCode")]
        public string CompanyCode { get; set; }

        [JsonProperty("consents")]
        public List<Base.Consent> Consents { get; set; }
    }
}
