using System.Text.Json.Serialization;

namespace IYSIntegration.Application.Services.Models
{
    public sealed class ConsentBatchErrorModel
    {
        [JsonPropertyName("errors")]
        public List<ConsentBatchErrorItem> Errors { get; set; } = new();
    }

    public sealed class ConsentBatchErrorItem
    {
        [JsonPropertyName("index")]
        public int? Index { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public sealed class ConsentErrorCompanyStats
    {
        [JsonPropertyName("companyCode")]
        public string? CompanyCode { get; set; }

        [JsonPropertyName("consentCount")]
        public int ConsentCount { get; set; }

        [JsonPropertyName("errorCount")]
        public int ErrorCount { get; set; }

        [JsonPropertyName("codes")]
        public List<ConsentErrorCodeStats> Codes { get; set; } = new();
    }

    public sealed class ConsentErrorCodeStats
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("messages")]
        public List<string> Messages { get; set; } = new();
    }
}
