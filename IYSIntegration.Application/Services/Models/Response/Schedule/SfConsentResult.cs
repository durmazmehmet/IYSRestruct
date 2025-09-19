namespace IYSIntegration.Application.Services.Models.Response.Schedule
{
    public class SfConsentResult
    {
        public long Id { get; set; }
        public bool IsSuccess { get; set; }
        public long LogId { get; set; }
        public string Error { get; set; }
    }
}
