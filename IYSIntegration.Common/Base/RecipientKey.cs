using Newtonsoft.Json;

namespace IYSIntegration.Common.Base
{
    public class RecipientKey
    {
        [JsonProperty("recipient", NullValueHandling = NullValueHandling.Ignore)]
        public string Recipient { get; set; }

        [JsonProperty("recipientType")]
        public string RecipientType { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
