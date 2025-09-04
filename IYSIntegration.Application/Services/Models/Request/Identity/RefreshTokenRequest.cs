using Newtonsoft.Json;

namespace IYSIntegration.Application.Request.Identity
{
    public class RefreshTokenRequest
    {
        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; }

        [JsonProperty("grant_type")]
        public string Granttype { get; set; }

    }
}
