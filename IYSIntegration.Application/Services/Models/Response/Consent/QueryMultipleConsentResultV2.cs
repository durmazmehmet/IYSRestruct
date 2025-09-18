using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Response.Consent;

public class QueryMultipleConsentResultV2
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

    [JsonProperty("creationDate", NullValueHandling = NullValueHandling.Ignore)]
    public string CreationDate { get; set; }

    [JsonProperty("isAddConsentV2Service", NullValueHandling = NullValueHandling.Ignore)]
    public bool? IsAddConsentV2Service { get; set; }

    [JsonProperty("originalRequestInformation", NullValueHandling = NullValueHandling.Ignore)]
    public OriginalRequestInformation OriginalRequestInformation { get; set; }
}

public class OriginalRequestInformation
{
    [JsonProperty("requestId", NullValueHandling = NullValueHandling.Ignore)]
    public string RequestId { get; set; }

    [JsonProperty("subRequestId", NullValueHandling = NullValueHandling.Ignore)]
    public string SubRequestId { get; set; }
}
