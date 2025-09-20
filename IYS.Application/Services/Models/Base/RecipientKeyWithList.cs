using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Base
{
    public class RecipientKeyWithList
    {
        [JsonProperty("recipients", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Recipients { get; set; }

        [JsonProperty("recipientType")]
        public string RecipientType { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

}
