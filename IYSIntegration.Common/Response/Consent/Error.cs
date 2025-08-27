using Newtonsoft.Json;

namespace IYSIntegration.Common.Response.Consent
{
    public partial class Error
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
