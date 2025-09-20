using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Base
{
    public class ConsentParams
    {
        [JsonProperty("iysCode")]
        public int IysCode { get; set; }

        [JsonProperty("brandCode")]
        public int BrandCode { get; set; }
    }
}
