using Newtonsoft.Json;

namespace IYSIntegration.Common.Request.Brand
{
    public class GetBrandRequest
    {
        [JsonProperty("iysCode")]
        public int IysCode { get; set; }
    }
}
