using IYS.Application.Services.Models.Base;
using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Request
{
    public class SfConsentBase
    {
        [JsonProperty("companyCode")]
        public string CompanyCode { get; set; }

        [JsonProperty("consents")]
        public List<Consent> Consents { get; set; }
    }
}
