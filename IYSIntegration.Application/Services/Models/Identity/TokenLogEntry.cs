namespace IYSIntegration.Application.Services.Models.Identity
{
    public sealed class TokenLogEntry
    {
        public string CompanyCode { get; set; } = string.Empty;

        public string? AccessTokenMasked { get; set; }

        public string? RefreshTokenMasked { get; set; }

        public DateTime TokenUpdateDateUtc { get; set; }
        public string Operation { get; set; } = string.Empty;
        public string ServerIdentifier { get; set; } = string.Empty;
    }
}
