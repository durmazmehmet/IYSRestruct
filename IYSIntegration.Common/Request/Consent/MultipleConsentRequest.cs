namespace IYSIntegration.Common.Request.Consent
{
    public class MultipleConsentRequest : Base.ConsentParams
    {
        public string CompanyCode { get; set; }
        public int BatchId { get; set; }
        public bool ForBatch { get; set; }
        public List<Common.Base.Consent> Consents { get; set; }

    }
}
