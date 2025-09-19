using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Response.Consent
{
    public class MultipleQueryConsentResult
    {

        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("list")]
        public List<string> List { get; set; }
    }
}
