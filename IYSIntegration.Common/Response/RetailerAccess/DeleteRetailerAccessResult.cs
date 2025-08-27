using Newtonsoft.Json;

namespace IYSIntegration.Common.Response.RetailerAccess
{
    public class DeleteRetailerAccessResult
    {
        [JsonProperty("requestId")]
        public string requestId { get; set; }

    }
}
