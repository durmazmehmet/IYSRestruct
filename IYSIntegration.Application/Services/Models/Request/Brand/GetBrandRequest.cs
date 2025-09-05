using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Request.Brand
{
    public class GetBrandRequest
    {
        [JsonProperty("iysCode")]
        public int IysCode { get; set; }
    }
}
