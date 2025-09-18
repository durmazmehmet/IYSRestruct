using IYSIntegration.Application.Services.Models.Base;
using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Request.Consent;

public class QueryMultipleConsentRequestV2 : ConsentParams
{
    [JsonProperty("companyCode")]
    public string CompanyCode { get; set; }

    [JsonProperty("batchId", NullValueHandling = NullValueHandling.Ignore)]
    public int? BatchId { get; set; }

    [JsonProperty("requestId")]
    public string? RequestId { get; set; }
}
