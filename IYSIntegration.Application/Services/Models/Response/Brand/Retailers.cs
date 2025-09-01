using Newtonsoft.Json;

namespace IYSIntegration.Application.Response.Brand
{
    public class Retailers
    {
        [JsonProperty("total")]
        public int Total { get; set; }
    }
}
