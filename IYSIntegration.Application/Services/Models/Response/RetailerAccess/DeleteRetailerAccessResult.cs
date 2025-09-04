using Newtonsoft.Json;

namespace IYSIntegration.Application.Response.RetailerAccess
{
    public class DeleteRetailerAccessResult
    {
        [JsonProperty("requestId")]
        public string requestId { get; set; }

    }
}
