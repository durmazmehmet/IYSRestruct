namespace IYS.Application.Services.Exceptions;

public class TokenRateLimitException : Exception
{
    public TokenRateLimitException(string errorCode, DateTime? haltUntilUtc, string message)
        : base(message)
    {
        ErrorCode = errorCode;
        HaltUntilUtc = haltUntilUtc;
    }

    public string ErrorCode { get; }

    public DateTime? HaltUntilUtc { get; }
}
