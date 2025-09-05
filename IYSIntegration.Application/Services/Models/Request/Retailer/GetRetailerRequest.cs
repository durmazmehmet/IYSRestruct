using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Request.Retailer
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
