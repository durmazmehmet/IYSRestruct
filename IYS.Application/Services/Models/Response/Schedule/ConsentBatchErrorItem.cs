using System.Text.Json.Serialization;

namespace IYS.Application.Services.Models.Response.Schedule
{
    public sealed class ConsentBatchErrorItem
    {
        [JsonPropertyName("index")]
        public int? Index { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
