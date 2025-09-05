using IYSIntegration.Application.Services.Models.Base;
using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Response.RetailerAccess
{
    public class QueryRetailerAccessResult
    {
        [JsonProperty("pagination")]
        public Pagination Pagination { get; set; }

        [JsonProperty("list")]
        public List<Base.Retailer> Retailers { get; set; }
    }
}
