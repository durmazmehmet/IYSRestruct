using System;

namespace IYS.Application.Services.Models.Base
{
    public class ConsentStateInfo
    {
        public string Recipient { get; set; } = string.Empty;

        public string? Status { get; set; }

        public DateTime? ConsentDate { get; set; }

        public bool HasStoredHistory { get; set; }

        public void ApplyStatus(string status, bool hasStoredHistory)
        {
            Status = status;
            HasStoredHistory = hasStoredHistory;
        }
    }
}
