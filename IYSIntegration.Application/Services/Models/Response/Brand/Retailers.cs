using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Response.Brand
{
    public class Retailers
    {
        [JsonProperty("total")]
        public int Total { get; set; }
    }
}
