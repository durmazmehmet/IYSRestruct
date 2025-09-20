using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Request
{
    public class SfConsentAddRequest
    {
        [JsonProperty("request")]
        public SfConsentBase Request { get; set; }
    }
}
