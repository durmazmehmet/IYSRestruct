namespace IYSIntegration.Application.Request.Consent
{
    public class MultipleConsentRequest : Base.ConsentParams
    {
        public string CompanyCode { get; set; }
        public int BatchId { get; set; }
        public bool ForBatch { get; set; }
        public List<Application.Base.Consent> Consents { get; set; }

    }
}
