namespace IYSIntegration.Application.Services
{
    public class LogResult
    {
        public long Id { get; set; }
        public string CompanyCode { get; set; }
        public string Status { get; set; } = string.Empty; // Success, Failed, Skipped, Exception
        public string? Message { get; set; }
    }
}
