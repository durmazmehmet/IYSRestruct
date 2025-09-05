using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Response.Consent
{
    public partial class MultipleConsentResult
    {
        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("subRequests")]
        public SubRequest[] SubRequests { get; set; }
    }
}
