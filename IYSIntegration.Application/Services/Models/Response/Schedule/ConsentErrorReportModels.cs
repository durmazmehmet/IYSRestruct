using System.Text.Json.Serialization;

namespace IYSIntegration.Application.Services.Models.Response.Schedule
{
    public sealed class ConsentBatchErrorModel
    {
        [JsonPropertyName("errors")]
        public List<ConsentBatchErrorItem> Errors { get; set; } = new();
    }
}
