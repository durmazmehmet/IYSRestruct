using Newtonsoft.Json;

namespace IYSIntegration.Application.Request.Identity
{
    public class Credential
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("grant_type")]
        public string Granttype { get; set; }
    }
}
