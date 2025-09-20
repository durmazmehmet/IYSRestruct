using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Response.RetailerAccess
{
    public class DeleteRetailerAccessResult
    {
        [JsonProperty("requestId")]
        public string requestId { get; set; }

    }
}
