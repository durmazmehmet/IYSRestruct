using System.Text.Json.Serialization;

namespace IYSIntegration.Application.Services.Models.Response.Schedule
{
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
