using Newtonsoft.Json;

namespace IYSIntegration.Application.Response.Consent
{
    public partial class MultipleConsentResult
    {
        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("subRequests")]
        public SubRequest[] SubRequests { get; set; }
    }
}
