namespace IYSIntegration.Common.Request.Consent
{
    public class AddConsentRequest : Base.ConsentParams
    {
        public Common.Base.Consent Consent { get; set; }
        public bool WithoutLogging { get; set; }
        public string? SalesforceId { get; set; }
        public string? CompanyCode { get; set; }
    }
}
