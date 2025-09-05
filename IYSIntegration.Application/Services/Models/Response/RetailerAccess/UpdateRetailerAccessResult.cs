using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Response.RetailerAccess
{
    public class UpdateRetailerAccessResult
    {
        [JsonProperty("requestId")]
        public string requestId { get; set; }

    }
}
