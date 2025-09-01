using Newtonsoft.Json;

namespace IYSIntegration.Application.Response.RetailerAccess
{
    public class UpdateRetailerAccessResult
    {
        [JsonProperty("requestId")]
        public string requestId { get; set; }

    }
}
