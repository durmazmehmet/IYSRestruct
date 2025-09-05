using IYSIntegration.Application.Services.Models.Base;
using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Request.RetailerAccess
{
    public class QueryRetailerAccessRequest : ConsentParams
    {
        [JsonProperty("offset")]
        public int? Offset { get; set; }

        [JsonProperty("limit")]
        public int? Limit { get; set; }

        public RecipientKey RecipientKey { get; set; }
    }
}
