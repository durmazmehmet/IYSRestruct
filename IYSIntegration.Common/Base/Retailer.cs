using Newtonsoft.Json;

namespace IYSIntegration.Common.Base
{
    public class Retailer
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("mersis")]
        public string Mersis { get; set; }

        [JsonProperty("alias")]
        public string Alias { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("tckn")]
        public string Tckn { get; set; }

        [JsonProperty("mobile")]
        public string Mobile { get; set; }

        [JsonProperty("city")]
        public City City { get; set; }

        [JsonProperty("town")]
        public City Town { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("retailerCode", NullValueHandling = NullValueHandling.Ignore)]
        public int? RetailerCode { get; set; }
    }
}
