using Newtonsoft.Json;

namespace IYSIntegration.Application.Request.Consent
{
    public class QueryMultipleConsentRequest : Base.ConsentParams
    {

        [JsonProperty("companyCode")]
        public string CompanyCode { get; set; }

        [JsonProperty("batchId")]
        public int BatchId { get; set; }

        [JsonProperty("requestId")]
        public string? RequestId { get; set; }
    }
}
