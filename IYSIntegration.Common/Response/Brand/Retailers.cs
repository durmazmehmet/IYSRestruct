using Newtonsoft.Json;

namespace IYSIntegration.Common.Response.Brand
{
    public class Retailers
    {
        [JsonProperty("total")]
        public int Total { get; set; }
    }
}
