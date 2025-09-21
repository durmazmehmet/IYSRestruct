using IYS.Application.Services.Models.Base;

namespace IYS.Application.Services.Models.Response.Consent
{
    public sealed class QueryConsentAggregationItem
    {
        public string CompanyCode { get; set; } = string.Empty;

        public string RecipientType { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public ResponseBase<QueryConsentResult>? Response { get; set; }
            = new ResponseBase<QueryConsentResult>();
    }
}
