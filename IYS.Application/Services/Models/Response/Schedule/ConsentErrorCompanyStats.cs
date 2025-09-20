using System.Text.Json.Serialization;

namespace IYS.Application.Services.Models.Response.Schedule
{
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
}
