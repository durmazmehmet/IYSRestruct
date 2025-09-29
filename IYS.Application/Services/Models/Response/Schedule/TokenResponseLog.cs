namespace IYS.Application.Services.Models.Response.Schedule;

public class TokenResponseLog
{
    public string IysCode { get; set; }
    public string TokenResponse { get; set; }
    public DateTime? HaltUntilUtc { get; set; }
}
