namespace IYS.Application.Services.Models.Response.Consent
{
    public class PullConsentSummary
    {
        public long Id { get; set; }

        public string? CompanyCode { get; set; }
        public DateTime ConsentDate { get; set; }
        public string? Recipient { get; set; }
        public string? RecipientType { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
        public bool IsProcessed { get; set; }
    }
}