using Newtonsoft.Json;

namespace IYSIntegration.Application.Services.Models.Response.Identity
{
    public class Token
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("expires_in")]
        public long ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("refresh_expires_in")]
        public long RefreshExpiresIn { get; set; }

        [JsonProperty("tokenValidTill")]
        public DateTime? TokenValidTill { get; set; }

        [JsonProperty("refreshTokenValidTill")]
        public DateTime? RefreshTokenValidTill { get; set; }

        [JsonProperty("tokenValidTillLocal")]
        public string? TokenValidTillLocal => FormatUtcPlus3Date(TokenValidTill);

        [JsonProperty("refreshTokenValidTillLocal")]
        public string? RefreshTokenValidTillLocal => FormatUtcPlus3Date(RefreshTokenValidTill);

        [JsonIgnore]
        public DateTime RefreshDate { get; set; }

        [JsonIgnore]
        public DateTime CreateDate { get; set; }
        public DateTime PreviousDate { get; internal set; }

        private static string? FormatUtcPlus3Date(DateTime? dateTime)
        {
            if (!dateTime.HasValue)
            {
                return null;
            }

            var utcDateTime = dateTime.Value;

            utcDateTime = utcDateTime.Kind switch
            {
                DateTimeKind.Unspecified => DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc),
                DateTimeKind.Local => utcDateTime.ToUniversalTime(),
                _ => utcDateTime
            };

            var utcPlusThree = utcDateTime.ToUniversalTime().AddHours(3);

            return $"{utcPlusThree:yyyy-MM-dd HH:mm:ss} (UTC+3)";
        }
    }
}
