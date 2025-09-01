using Newtonsoft.Json;

namespace IYSIntegration.Application.Response.Consent
{
    public class Stats
    {
        [JsonProperty("ONAY")]
        public int Onay { get; set; }

        [JsonProperty("RET")]
        public int Ret { get; set; }

        [JsonProperty("BEKLIYOR")]
        public int Bekliyor { get; set; }

        [JsonProperty("TOTAL")]
        public int Total { get; set; }
    }
}
