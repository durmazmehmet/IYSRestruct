using IYS.Application.Services.Models.Response.Schedule;
using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Base
{
    public class IysBaseConsent
    {
        [JsonProperty("consentDate")]
        public string ConsentDate { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("recipient")]
        public string Recipient { get; set; }

        [JsonProperty("recipientType")]
        public string RecipientType { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("retailerCode", NullValueHandling = NullValueHandling.Ignore)]
        public int? RetailerCode { get; set; }

        [JsonProperty("retailerAccess", NullValueHandling = NullValueHandling.Ignore)]
        public int[]? RetailerAccess { get; set; }
    }

    public class Consent : IysBaseConsent
    {
        [JsonProperty("creationDate", NullValueHandling = NullValueHandling.Ignore)]
        public string? CreationDate { get; set; }

        [JsonProperty("transactionId", NullValueHandling = NullValueHandling.Ignore)]
        public string? TransactionId { get; set; }

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
        [System.Text.Json.Serialization.JsonIgnore]
        public string? BatchError { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("batchError")]
        public ConsentBatchErrorModel? BatchErrorModel { get; set; }

        [JsonIgnore]
        public string? CompanyCode { get; set; }

        [JsonIgnore]
        public DateTime? CreateDate { get; set; }
    }
}
