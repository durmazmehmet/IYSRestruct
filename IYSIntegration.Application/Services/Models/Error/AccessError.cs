using Newtonsoft.Json;

namespace IYSIntegration.Application.Error
{
    public class AccessError
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }
    }
}
