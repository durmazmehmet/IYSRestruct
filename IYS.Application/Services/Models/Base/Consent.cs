using IYS.Application.Services.Models.Response.Schedule;
using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Base;

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
    public long? BatchId { get; set; }

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

