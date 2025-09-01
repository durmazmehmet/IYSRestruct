using System.Collections.Generic;

namespace IYSIntegration.Application.Services.Models
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
