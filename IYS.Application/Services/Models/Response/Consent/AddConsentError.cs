using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Response.Consent
{
    public class AddConsentError
    {

        [JsonProperty("code")]
        public string? Code { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }
    }

}
