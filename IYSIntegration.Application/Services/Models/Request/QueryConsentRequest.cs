using IYSIntegration.Application.Services.Models.Base;

namespace IYSIntegration.Application.Services.Models.Request
{
    public class QueryConsentRequest : ConsentParams
    {
        public string CompanyCode { get; set; }
        public RecipientKey RecipientKey { get; set; }
    }

    public class QueryMutipleConsentRequest : ConsentParams
    {
        public string CompanyCode { get; set; }
        public RecipientKeyWithList RecipientKeyWithList { get; set; }
    }
}
