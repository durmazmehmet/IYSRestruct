using Newtonsoft.Json;

namespace IYSIntegration.Common.Response.Identity
{
    public class Token
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("expires_in")]
        public long ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("refresh_expires_in")]
        public long RefreshExpiresIn { get; set; }

        [JsonProperty("tokenValidTill")]
        public DateTime? TokenValidTill { get; set; }

        [JsonProperty("refreshTokenValidTill")]
        public DateTime? RefreshTokenValidTill { get; set; }
    }
}
