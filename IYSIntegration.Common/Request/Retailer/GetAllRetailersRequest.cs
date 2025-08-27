using Newtonsoft.Json;

namespace IYSIntegration.Common.Request.Retailer
{
    public class GetAllRetailersRequest : Base.ConsentParams
    {
        [JsonProperty("offset")]
        public int? Offset { get; set; }

        [JsonProperty("limit")]
        public int? Limit { get; set; }
    }
}
