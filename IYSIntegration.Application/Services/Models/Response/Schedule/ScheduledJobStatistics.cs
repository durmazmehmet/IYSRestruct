namespace IYSIntegration.Application.Services.Models.Response.Schedule
{
    /// <summary>
    /// Represents simple statistics for scheduled jobs.
    /// </summary>
    public class ScheduledJobStatistics
    {
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> FailedCompanyCodes { get; set; } = new();
    }
}
