using Newtonsoft.Json;

namespace IYSIntegration.Application.Request.Consent
{
    public class SfConsentAddRequest
    {
        [JsonProperty("request")]
        public SfConsentBase Request { get; set; }
    }
}
