using Newtonsoft.Json;

namespace IYSIntegration.Common.Base
{
    public class ConsentParams
    {
        [JsonProperty("iysCode")]
        public int IysCode { get; set; }

        [JsonProperty("brandCode")]
        public int BrandCode { get; set; }
    }
}
