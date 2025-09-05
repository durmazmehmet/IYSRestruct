using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Response.Retailer
{
    public class GetRetailerResponse : Base.Retailer
    {
        [JsonProperty("retailerAccessCount")]
        public int RetailerAccessCount { get; set; }

        [JsonProperty("canBeDeleted")]
        public bool CanBeDeleted { get; set; }
    }
}
