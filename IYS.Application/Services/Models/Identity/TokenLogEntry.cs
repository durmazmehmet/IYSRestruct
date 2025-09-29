using System;

namespace IYS.Application.Services.Models.Identity
{
    public sealed class TokenLogEntry
    {
        public string CompanyCode { get; set; } = string.Empty;

        public string? AccessTokenMasked { get; set; }

        public string? RefreshTokenMasked { get; set; }

        public DateTime TokenCreateDateUtc { get; set; }

        public DateTime? TokenRefreshDateUtc { get; set; }

        public string Operation { get; set; } = string.Empty;
        public string ServerIdentifier { get; set; } = string.Empty;
    }
}
