using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Response.Retailer
{
    public class GetRetailerResponse : Base.RetailerModel
    {
        [JsonProperty("retailerAccessCount")]
        public int RetailerAccessCount { get; set; }

        [JsonProperty("canBeDeleted")]
        public bool CanBeDeleted { get; set; }
    }
}
