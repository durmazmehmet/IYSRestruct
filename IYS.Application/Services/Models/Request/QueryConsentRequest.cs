using IYS.Application.Services.Models.Base;

namespace IYS.Application.Services.Models.Request
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
