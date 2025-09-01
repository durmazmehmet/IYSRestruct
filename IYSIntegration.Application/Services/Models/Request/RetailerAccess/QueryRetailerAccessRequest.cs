using IYSIntegration.Application.Base;
using Newtonsoft.Json;

namespace IYSIntegration.Application.Request.RetailerAccess
{
    public class QueryRetailerAccessRequest : Application.Base.ConsentParams
    {
        [JsonProperty("offset")]
        public int? Offset { get; set; }

        [JsonProperty("limit")]
        public int? Limit { get; set; }

        public RecipientKey RecipientKey { get; set; }
    }
}
