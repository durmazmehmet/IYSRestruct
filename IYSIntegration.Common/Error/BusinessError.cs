using Newtonsoft.Json;

namespace IYSIntegration.Common.Error
{
    public class BusinessError
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
