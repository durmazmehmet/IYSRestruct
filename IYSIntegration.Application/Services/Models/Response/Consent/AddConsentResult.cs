using Newtonsoft.Json;

namespace IYSIntegration.Application.Response.Consent
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


    public class AddConsentError
    {

        [JsonProperty("code")]
        public string? Code { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }
    }

}
