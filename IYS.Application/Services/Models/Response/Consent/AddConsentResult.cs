using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Response.Consent
{
    public class AddConsentResult
    {
        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }

        [JsonProperty("creationDate")]
        public string CreationDate { get; set; }

        [JsonProperty("errors")]
        public List<AddConsentError>? Errors { get; set; }
    }

}
