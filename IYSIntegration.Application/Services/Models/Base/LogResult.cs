namespace IYSIntegration.Application.Services.Models.Base
{
    public class LogResult
    {
        public long Id { get; set; }
        public string CompanyCode { get; set; }
        public string Status { get; set; } = string.Empty; // Success, Failed, Skipped, Exception
        public Dictionary<string, string> Messages { get; set; }

        public Dictionary<string, string> GetMessages()
        {
            var baseKey = GetBaseKey();

            if (Messages == null || Messages.Count == 0)
            {
                return new Dictionary<string, string>
                {
                    { $"info<{baseKey}>", "No messages" }
                };
            }

            var result = new Dictionary<string, string>();

            foreach (var message in Messages)
            {
                result[$"{baseKey}.{message.Key}"] = message.Value;
            }

            return result;
        }

        private string GetBaseKey()
        {
            string baseKey = string.Empty;

            if (!string.IsNullOrWhiteSpace(CompanyCode) && Id > 0)
                baseKey = $"{CompanyCode},{Id}";
            else if (!string.IsNullOrWhiteSpace(CompanyCode))
                baseKey = CompanyCode;
            else if (Id > 0)
                baseKey = Id.ToString();
            else
                baseKey = "Unknown";

            return baseKey;
        }


    }
}
