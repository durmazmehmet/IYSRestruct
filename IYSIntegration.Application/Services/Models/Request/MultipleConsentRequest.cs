using IYSIntegration.Application.Services.Models.Base;

namespace IYSIntegration.Application.Services.Models.Request
{
    public class MultipleConsentRequest : ConsentParams
    {
        public string? CompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public int BatchId { get; set; }
        public bool ForBatch { get; set; }
        public List<Base.Consent> Consents { get; set; }

    }
}
