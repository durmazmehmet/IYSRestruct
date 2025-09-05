using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Response.Retailer
{
    public class DeleteRetailerResponse : Base.Retailer
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
