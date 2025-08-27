using IYSIntegration.Common.Base;

namespace IYSIntegration.WorkerService.Models
{
    public class BatchSummary : ConsentParams
    {
        public int BatchId { get; set; }
        public int Count { get; set; }

    }
}
