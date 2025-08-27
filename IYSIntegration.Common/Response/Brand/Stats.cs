using Newtonsoft.Json;

namespace IYSIntegration.Common.Response.Brand
{
    public class Stats
    {
        [JsonProperty("consents")]
        public Consents Consents { get; set; }

        [JsonProperty("retailers")]
        public Retailers Retailers { get; set; }
    }
}
