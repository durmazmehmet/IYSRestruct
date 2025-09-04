namespace IYSIntegration.Application.Request.Consent
{
    public class AddConsentRequest : Base.ConsentParams
    {
        public Application.Base.Consent Consent { get; set; }
        public bool WithoutLogging { get; set; }
        public string? SalesforceId { get; set; }
        public string? CompanyCode { get; set; }
    }
}
