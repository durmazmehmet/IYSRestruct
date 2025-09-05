using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Base
{
    public class City
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
