using Newtonsoft.Json;

namespace IYSIntegration.Common.Request.Retailer
{
    public class GetRetailerRequest
    {
        [JsonProperty("brandCode")]
        public int BrandCode { get; set; }

        [JsonProperty("iysCode")]
        public int IysCode { get; set; }

        [JsonProperty("retailerCode")]
        public int RetailerCode { get; set; }
    }
}
