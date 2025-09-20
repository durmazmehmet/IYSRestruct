using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Response.Retailer
{
    public class DeleteRetailerResponse : Base.RetailerModel
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
