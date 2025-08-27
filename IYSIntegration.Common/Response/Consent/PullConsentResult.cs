using Newtonsoft.Json;

namespace IYSIntegration.Common.Response.Consent
{
    public class PullConsentResult
    {
        [JsonProperty("after")]
        public string After { get; set; }

        [JsonProperty("list")]
        public Common.Base.Consent[] List { get; set; }
    }
}
