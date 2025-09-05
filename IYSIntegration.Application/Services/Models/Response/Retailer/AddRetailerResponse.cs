using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Response.Retailer
{
    public class AddRetailerResponse
    {
        [JsonProperty("retailerCode")]
        public int RetailerCode { get; set; }
    }
}
