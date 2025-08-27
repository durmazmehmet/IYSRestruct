using Newtonsoft.Json;

namespace IYSIntegration.Common.Base
{
    public class Pagination
    {
        [JsonProperty("offset")]
        public long Offset { get; set; }

        [JsonProperty("pageSize")]
        public long PageSize { get; set; }

        [JsonProperty("totalCount", NullValueHandling = NullValueHandling.Ignore)]
        public int? TotalCount { get; set; }
    }
}
