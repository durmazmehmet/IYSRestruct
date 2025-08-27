using Newtonsoft.Json;

namespace IYSIntegration.Common.Response.Consent
{
    public class QueryConsentResult : Base.Consent
    {
        [JsonProperty("creationDate")]
        public string CreationDate { get; set; }

        [JsonProperty("retailerAccessCount")]
        public int RetailerAccessCount { get; set; }

        [JsonProperty("retailerTitle")]
        public string RetailerTitle { get; set; }
    }
}
