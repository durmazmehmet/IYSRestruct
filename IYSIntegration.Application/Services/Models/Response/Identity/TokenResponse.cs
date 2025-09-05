using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Response.Identity
{
    public class TokenResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("status")]
        public long Status { get; set; }

        [JsonProperty("result")]
        public Token Result { get; set; }
    }
}
