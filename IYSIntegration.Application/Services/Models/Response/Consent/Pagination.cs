using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Response.Consent
{
    public class Pagination
    {
        [JsonProperty("after")]
        public int After { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }
    }
}
