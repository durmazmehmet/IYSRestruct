using IYSIntegration.Common.Base;
using Newtonsoft.Json;

namespace IYSIntegration.Common.Response.Retailer
{
    public class GetAllRetailersResponse
    {
        [JsonProperty("pagination")]
        public Pagination Pagination { get; set; }

        [JsonProperty("list")]
        public GetRetailerResponse[] List { get; set; }
    }
}
