using Newtonsoft.Json;

namespace IYSIntegration.Common.Response.Brand
{
    public class Brand
    {
        [JsonProperty("brandCode")]
        public long BrandCode { get; set; }

        [JsonProperty("stats")]
        public Stats Stats { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("master")]
        public bool Master { get; set; }
    }
}
