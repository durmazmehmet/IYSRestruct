using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Response.Brand
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
