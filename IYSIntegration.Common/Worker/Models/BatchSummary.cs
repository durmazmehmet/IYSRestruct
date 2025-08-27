using IYSIntegration.Common.Base;

namespace IYSIntegration.Common.Worker.Models
{
    public class BatchSummary : ConsentParams
    {
        public int BatchId { get; set; }
        public int Count { get; set; }

    }
}
