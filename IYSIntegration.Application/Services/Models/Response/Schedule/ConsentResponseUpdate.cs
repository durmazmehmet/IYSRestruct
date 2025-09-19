namespace IYSIntegration.Application.Services.Models.Response.Schedule
{
    public class ConsentResponseUpdate
    {
        public long Id { get; set; }
        public long LogId { get; set; }
        public bool IsSuccess { get; set; }
        public string? TransactionId { get; set; }
        public string? CreationDate { get; set; }
        public string? BatchError { get; set; }
        public bool IsOverdue { get; set; }
    }
}
