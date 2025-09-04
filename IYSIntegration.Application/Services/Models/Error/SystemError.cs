using Newtonsoft.Json;

namespace IYSIntegration.Application.Error
{
    public class SystemError : BusinessError
    {
        [JsonProperty("index")]
        public int Index { get; set; }

    }
}
