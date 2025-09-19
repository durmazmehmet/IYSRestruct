namespace IYSIntegration.Application.Services.Models.Response.Consent
{
    public class PullConsentSummary
    {
        public long Id { get; set; }
        public string? Recipient { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
    }
}
