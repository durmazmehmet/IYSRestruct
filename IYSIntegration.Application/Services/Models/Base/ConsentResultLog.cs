namespace IYSIntegration.Application.Base
{
    public class ConsentResultLog
    {
        public long Id { get; set; }
        public long LogId { get; set; }
        public bool IsProcessed { get; set; }
        public bool IsSuccess { get; set; }
        public string Response { get; set; }
        public string SendDate { get; set; }
    }
}
