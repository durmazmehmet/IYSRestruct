using Newtonsoft.Json;

namespace IYSIntegration.Application.Response.RetailerAccess
{
    public class AddRetailerAccessResult
    {
        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }

    }
}
