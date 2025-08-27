using IYSIntegration.Common.Base;

namespace IYSIntegration.Common.Request.Consent
{
    public class QueryConsentRequest : Base.ConsentParams
    {
        public string CompanyCode { get; set; }
        public RecipientKey RecipientKey { get; set; }
    }
}
