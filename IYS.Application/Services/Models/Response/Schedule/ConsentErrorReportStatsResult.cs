using System.Text.Json.Serialization;

namespace IYS.Application.Services.Models.Response.Schedule
{
    public sealed class ConsentErrorReportStatsResult
    {
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

        [JsonPropertyName("dataRangeStart")]
        public DateTime? DataRangeStart { get; set; }

        [JsonPropertyName("dataRangeEnd")]
        public DateTime? DataRangeEnd { get; set; }

        [JsonPropertyName("companies")]
        public List<ConsentErrorCompanyStats> Companies { get; set; } = new();
    }
}
