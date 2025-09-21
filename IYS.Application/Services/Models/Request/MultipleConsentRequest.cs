using IYS.Application.Services.Models.Base;

namespace IYS.Application.Services.Models.Request
{
    public class MultipleConsentRequest : ConsentParams
    {
        public string? CompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public long? BatchId { get; set; }
        public bool ForBatch { get; set; }
        public List<Consent> Consents { get; set; }

    }
}
