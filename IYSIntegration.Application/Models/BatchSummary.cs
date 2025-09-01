using IYSIntegration.Common.Base;

namespace IYSIntegration.Application.Models
{
    public class BatchSummary : ConsentParams
    {
        public int BatchId { get; set; }
        public int Count { get; set; }

    }
}
