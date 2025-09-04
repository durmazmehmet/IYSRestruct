using Newtonsoft.Json;

namespace IYSIntegration.Application.Response.Brand
{
    public class Consents
    {
        [JsonProperty("approval")]
        public long Approval { get; set; }

        [JsonProperty("rejection")]
        public long Rejection { get; set; }

        [JsonProperty("total")]
        public long Total { get; set; }
    }
}
