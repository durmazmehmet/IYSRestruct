using IYS.Application.Services.Models.Base;
using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Response.RetailerAccess
{
    public class QueryRetailerAccessResult
    {
        [JsonProperty("pagination")]
        public Pagination Pagination { get; set; }

        [JsonProperty("list")]
        public List<RetailerModel> Retailers { get; set; }
    }
}
