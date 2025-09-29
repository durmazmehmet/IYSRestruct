using System.Text.Json.Serialization;

namespace IYS.Application.Services.Models.Response.Schedule
{
    public sealed class ConsentErrorReportStatsResult
    {
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

        [JsonPropertyName("dataRangeStart")]
        public string DataRangeStart { get; set; }

        [JsonPropertyName("dataRangeEnd")]
        public string DataRangeEnd { get; set; }

        [JsonPropertyName("companies")]
        public List<ConsentErrorCompanyStats> Companies { get; set; } = new();
    }
}
