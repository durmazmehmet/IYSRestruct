using Newtonsoft.Json;

namespace IYSIntegration.Common.Response.RetailerAccess
{
    public class UpdateRetailerAccessResult
    {
        [JsonProperty("requestId")]
        public string requestId { get; set; }

    }
}
