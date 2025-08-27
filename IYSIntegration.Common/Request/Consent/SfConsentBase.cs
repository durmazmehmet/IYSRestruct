using Newtonsoft.Json;

namespace IYSIntegration.Common.Request.Consent
{
    public class SfConsentBase
    {
        [JsonProperty("companyCode")]
        public string CompanyCode { get; set; }

        [JsonProperty("consents")]
        public List<Common.Base.Consent> Consents { get; set; }
    }
}
