using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Base
{
    public class RetailerRecipientAccess : RecipientKey
    {
        [JsonProperty("retailerAccess", NullValueHandling = NullValueHandling.Ignore)]
        public List<int> RetailerAccess { get; set; }

        [JsonProperty("recipients", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Recipients { get; set; }

    }
}
