using System.Text.Json.Serialization;

namespace IYS.Application.Services.Models.Response.Schedule
{
    public sealed class ConsentBatchErrorModel
    {
        [JsonPropertyName("errors")]
        public List<ConsentBatchErrorItem> Errors { get; set; } = new();
    }
}
