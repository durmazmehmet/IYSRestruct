using Newtonsoft.Json;

namespace IYSIntegration.Application.Response.RetailerAccess
{
    public class QueryRetailerAccessResult
    {
        [JsonProperty("pagination")]
        public Base.Pagination Pagination { get; set; }

        [JsonProperty("list")]
        public List<Base.Retailer> Retailers { get; set; }
    }
}
