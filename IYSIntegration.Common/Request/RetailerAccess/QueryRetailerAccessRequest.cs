using IYSIntegration.Common.Base;
using Newtonsoft.Json;

namespace IYSIntegration.Common.Request.RetailerAccess
{
    public class QueryRetailerAccessRequest : Common.Base.ConsentParams
    {
        [JsonProperty("offset")]
        public int? Offset { get; set; }

        [JsonProperty("limit")]
        public int? Limit { get; set; }

        public RecipientKey RecipientKey { get; set; }
    }
}
