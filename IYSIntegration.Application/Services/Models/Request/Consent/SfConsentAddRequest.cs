using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Request.Consent
{
    public class SfConsentAddRequest
    {
        [JsonProperty("request")]
        public SfConsentBase Request { get; set; }
    }
}
