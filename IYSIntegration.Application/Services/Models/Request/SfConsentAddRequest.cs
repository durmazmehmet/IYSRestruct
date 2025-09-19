using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Request
{
    public class SfConsentAddRequest
    {
        [JsonProperty("request")]
        public SfConsentBase Request { get; set; }
    }
}
