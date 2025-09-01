using Newtonsoft.Json;

namespace IYSIntegration.Application.Response.Consent
{
    public class PullConsentResult
    {
        [JsonProperty("after")]
        public string After { get; set; }

        [JsonProperty("list")]
        public Application.Base.Consent[] List { get; set; }
    }
}
