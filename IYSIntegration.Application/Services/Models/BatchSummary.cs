using IYSIntegration.Application.Base;

namespace IYSIntegration.Application.Services.Models
{
    public class BatchSummary : ConsentParams
    {
        public int BatchId { get; set; }
        public int Count { get; set; }

    }
}
