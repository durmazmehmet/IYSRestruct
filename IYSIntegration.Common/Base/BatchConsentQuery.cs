namespace IYSIntegration.Common.Base
{
    public class BatchConsentQuery : ConsentParams
    {
        public int BatchId { get; set; }
        public string RequestId { get; set; }
        public long LogId { get; set; }
        public int CheckAfter { get; set; }
    }
}
