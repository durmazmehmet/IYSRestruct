using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Response.Consent
{
    public partial class QueryMultipleConsentResult
    {
        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("subRequestId")]
        public string SubrequestId { get; set; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public Error Error { get; set; }

        [JsonProperty("transactionId", NullValueHandling = NullValueHandling.Ignore)]
        public string TransactionId { get; set; }

        [JsonProperty("requestId", NullValueHandling = NullValueHandling.Ignore)]
        public string RequestId { get; set; }
    }
}
