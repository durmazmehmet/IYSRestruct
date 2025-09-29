using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Base;

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
