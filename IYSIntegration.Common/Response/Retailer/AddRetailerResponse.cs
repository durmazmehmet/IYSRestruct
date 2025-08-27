using Newtonsoft.Json;

namespace IYSIntegration.Common.Response.Retailer
{
    public class AddRetailerResponse
    {
        [JsonProperty("retailerCode")]
        public int RetailerCode { get; set; }
    }
}
