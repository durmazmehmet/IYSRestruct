using Newtonsoft.Json;

namespace IYSIntegration.Application.Response.Retailer
{
    public class AddRetailerResponse
    {
        [JsonProperty("retailerCode")]
        public int RetailerCode { get; set; }
    }
}
