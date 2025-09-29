using IYS.Application.Services.Models.Response.Schedule;
using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Base;

public class ConsentErrorModel
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("companyCode")]
    public string? CompanyCode { get; set; }

    [JsonProperty("salesforceId")]
    public string? SalesforceId { get; set; }

    [JsonProperty("iysCode")]
    public string? IysCode { get; set; }

    [JsonProperty("brandCode")]
    public string? BrandCode { get; set; }

    [JsonProperty("consentDate")]
    public string? ConsentDate { get; set; }

    [JsonProperty("creationDate")]
    public string? CreationDate { get; set; }

    [JsonProperty("source")]
    public string? Source { get; set; }

    [JsonProperty("recipient")]
    public string? Recipient { get; set; }

    [JsonProperty("recipientType")]
    public string? RecipientType { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("transactionId")]
    public string? TransactionId { get; set; }

    [JsonProperty("isSuccess")]
    public bool? IsSuccess { get; set; }

    [JsonProperty("updateDate")]
    public DateTime? UpdateDate { get; set; }

    [JsonProperty("logId")]
    public long? LogId { get; set; }

    [JsonProperty("isProcessed")]
    public bool? IsProcessed { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string? BatchError { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("batchError")]
    public ConsentBatchErrorModel? BatchErrorModel { get; set; }

    [JsonProperty("createDate")]
    public string? CreateDate { get; set; }
}

