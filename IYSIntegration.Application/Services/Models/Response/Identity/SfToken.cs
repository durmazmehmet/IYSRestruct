using Newtonsoft.Json;


namespace IYSIntegration.Application.Response.Identity
{
    public class SfToken
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("instance_url")]
        public Uri InstanceUrl { get; set; }

        [JsonProperty("id")]
        public Uri Id { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("issued_at")]
        public string IssuedAt { get; set; }

        [JsonProperty("signature")]
        public string Signature { get; set; }
    }
}
