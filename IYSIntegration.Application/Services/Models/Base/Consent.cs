using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Base
{
    public class Consent
    {
        [JsonProperty("recipient")]
        public string Recipient { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("consentDate")]
        public string ConsentDate { get; set; }

        [JsonProperty("recipientType")]
        public string RecipientType { get; set; }

        [JsonProperty("totalCount", NullValueHandling = NullValueHandling.Ignore)]
        public int RetailerCode { get; set; }

        [JsonProperty("creationDate", NullValueHandling = NullValueHandling.Ignore)]
        public string? CreationDate { get; set; }

        [JsonProperty("transactionId", NullValueHandling = NullValueHandling.Ignore)]
        public string? TransactionId { get; set; }

        [JsonProperty("retailerAccess", NullValueHandling = NullValueHandling.Ignore)]
        public int[]? RetailerAccess { get; set; }

        [JsonIgnore]
        public long Id { get; set; }

        [JsonIgnore]
        public string? SalesforceId { get; set; }

        [JsonIgnore]
        public int? BatchId { get; set; }

        [JsonIgnore]
        public int? Index { get; set; }

        [JsonIgnore]
        public long? LogId { get; set; }

        [JsonIgnore]
        public bool? IsSuccess { get; set; }

        [JsonIgnore]
        public string? BatchError { get; set; }

        [JsonIgnore]
        public string? CompanyCode { get; set; }

        [JsonIgnore]
        public DateTime? CreateDate { get; set; }
    }
}
