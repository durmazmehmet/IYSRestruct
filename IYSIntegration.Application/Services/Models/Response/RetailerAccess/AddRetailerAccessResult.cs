using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Response.RetailerAccess
{
    public class AddRetailerAccessResult
    {
        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }

    }
}
