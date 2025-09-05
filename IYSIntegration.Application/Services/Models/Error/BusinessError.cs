using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Error
{
    public class BusinessError
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
