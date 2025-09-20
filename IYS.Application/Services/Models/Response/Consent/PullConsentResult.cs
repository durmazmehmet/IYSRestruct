using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Response.Consent
{
    public class PullConsentResult
    {
        [JsonProperty("after")]
        public string After { get; set; }

        [JsonProperty("list")]
        public Base.Consent[] List { get; set; }
    }
}
