using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Error
{
    public class AccessError
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }
    }
}
