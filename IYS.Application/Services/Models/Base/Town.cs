using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Base
{
    public class Town
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("city_code")]
        public string CityCode { get; set; }
    }
}
