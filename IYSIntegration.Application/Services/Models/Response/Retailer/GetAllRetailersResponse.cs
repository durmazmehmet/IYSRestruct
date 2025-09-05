using IYSIntegration.Application.Services.Models.Base;
using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Response.Retailer
{
    public class GetAllRetailersResponse
    {
        [JsonProperty("pagination")]
        public Pagination Pagination { get; set; }

        [JsonProperty("list")]
        public GetRetailerResponse[] List { get; set; }
    }
}
