using Newtonsoft.Json;

namespace IYS.Application.Services.Models.Error
{
    public class GenericError
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("errors")]
        public ErrorDetails[] Errors { get; set; }
    }
}
