using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Response.RetailerAccess
{
    public class AddRetailerAccessResult
    {
        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }

    }
}
