using IYSIntegration.Application.Base;

namespace IYSIntegration.Application.Request.Consent
{
    public class QueryConsentRequest : Base.ConsentParams
    {
        public string CompanyCode { get; set; }
        public RecipientKey RecipientKey { get; set; }
    }
}
