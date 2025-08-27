using Newtonsoft.Json;

namespace IYSIntegration.Common.Request.Consent
{
    public class SfConsentAddRequest
    {
        [JsonProperty("request")]
        public SfConsentBase Request { get; set; }
    }
}
