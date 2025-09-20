using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Identity
{
    public class RefreshTokenRequest
    {
        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; }

        [JsonProperty("grant_type")]
        public string Granttype { get; set; }

    }
}
