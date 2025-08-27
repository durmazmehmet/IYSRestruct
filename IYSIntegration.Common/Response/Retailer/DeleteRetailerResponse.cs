using Newtonsoft.Json;

namespace IYSIntegration.Common.Response.Retailer
{
    public class DeleteRetailerResponse : Base.Retailer
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
