using Newtonsoft.Json;

namespace IYSIntegration.Application.Request.Consent
{
    public class SfConsentBase
    {
        [JsonProperty("companyCode")]
        public string CompanyCode { get; set; }

        [JsonProperty("consents")]
        public List<Application.Base.Consent> Consents { get; set; }
    }
}
