using Newtonsoft.Json;

namespace IYSIntegration.Common.Response.RetailerAccess
{
    public class AddRetailerAccessResult
    {
        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }

    }
}
