using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Error
{
    [Serializable]
    public class ErrorDetails
    {
        [JsonProperty("index", DefaultValueHandling = DefaultValueHandling.Populate)]
        public int Index { get; set; }

        [JsonProperty("location")]
        public string[] Location { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

    }
}
