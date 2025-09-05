using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Request.Retailer
{
    public class AddRetailerRequest
    {
        [JsonProperty("iysCode")]
        public int IysCode { get; set; }

        [JsonProperty("brandCode")]
        public int BrandCode { get; set; }

        public Base.Retailer Retailer { get; set; }
    }
}
