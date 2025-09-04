namespace IYSIntegration.Application.Request.Consent
{
    public class PullConsentRequest : Base.ConsentParams
    {
        public string CompanyCode { get; set; }
        public string? After { get; set; }
        public int? Limit { get; set; }
        public string Source { get; set; }
    }
}
