using System.Collections.Generic;

namespace IYSIntegration.Common.Worker.Models
{
    public class ProcessResult
    {
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
