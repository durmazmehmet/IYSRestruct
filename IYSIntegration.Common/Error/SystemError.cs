using Newtonsoft.Json;

namespace IYSIntegration.Common.Error
{
    public class SystemError : BusinessError
    {
        [JsonProperty("index")]
        public int Index { get; set; }

    }
}
