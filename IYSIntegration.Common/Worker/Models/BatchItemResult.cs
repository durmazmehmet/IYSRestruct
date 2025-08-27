namespace IYSIntegration.Common.Worker.Models
{
    public class BatchItemResult
    {
        public int BatchId { get; set; }
        public int Index { get; set; }
        public bool IsSuccess { get; set; }
        public string BatchError { get; set; }
        public long LogId { get; set; }
        public bool IsQueryResult { get; set; }
    }
}
