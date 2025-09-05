using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Error
{
    public class SystemError : BusinessError
    {
        [JsonProperty("index")]
        public int Index { get; set; }

    }
}
