using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Response.Consent
{
    public class SfConsentAddResponse
    {
        [JsonProperty("wsStatus")]
        public string WsStatus { get; set; }

        [JsonProperty("wsDescription")]
        public string WsDescription { get; set; }

        [JsonProperty("consents")]
        public List<Base.Consent> Consents { get; set; }

        [JsonProperty("companyCode")]
        public string CompanyCode { get; set; }

        [JsonProperty("logid")]
        public long LogId { get; set; }


    }

    public class SfConsentAddErrorResponse
    {
        [JsonProperty("message")]
        public string message { get; set; }

        [JsonProperty("errorCode")]
        public string errorCode { get; set; }



    }
}
