using IYS.Application.Services.Models.Base;

namespace IYS.Application.Services.Models.Request
{
    public class PullConsentRequest : ConsentParams
    {
        public string CompanyCode { get; set; }
        public string? After { get; set; }
        public int? Limit { get; set; }
        public string Source { get; set; }
    }
}
