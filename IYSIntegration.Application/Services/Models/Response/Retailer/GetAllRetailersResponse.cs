using IYSIntegration.Application.Base;
using Newtonsoft.Json;

namespace IYSIntegration.Application.Response.Retailer
{
    public class GetAllRetailersResponse
    {
        [JsonProperty("pagination")]
        public Pagination Pagination { get; set; }

        [JsonProperty("list")]
        public GetRetailerResponse[] List { get; set; }
    }
}
