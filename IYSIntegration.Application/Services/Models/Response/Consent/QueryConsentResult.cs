using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Response.Consent
{
    public class QueryConsentResult : Base.IysBaseConsent
    {
        [JsonProperty("creationDate")]
        public string CreationDate { get; set; }

        [JsonProperty("retailerAccessCount")]
        public int RetailerAccessCount { get; set; }

        [JsonProperty("retailerTitle")]
        public string RetailerTitle { get; set; }

        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }
    }
}
