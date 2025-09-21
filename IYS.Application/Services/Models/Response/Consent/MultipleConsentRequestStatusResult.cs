using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Response.Consent
{
    public class MultipleConsentRequestStatusResult
    {
        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("subRequests")]
        public MultipleConsentRequestStatusSubRequest[] SubRequests { get; set; }
    }

    public class MultipleConsentRequestStatusSubRequest
    {
        [JsonProperty("requestId", NullValueHandling = NullValueHandling.Ignore)]
        public string RequestId { get; set; }

        [JsonProperty("subRequestId", NullValueHandling = NullValueHandling.Ignore)]
        public string SubRequestId { get; set; }

        [JsonProperty("index", NullValueHandling = NullValueHandling.Ignore)]
        public int? Index { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("transactionId", NullValueHandling = NullValueHandling.Ignore)]
        public string TransactionId { get; set; }

        [JsonProperty("creationDate", NullValueHandling = NullValueHandling.Ignore)]
        public string CreationDate { get; set; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public Error Error { get; set; }
    }
}
