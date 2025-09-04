namespace IYSIntegration.Application.Base;

public class MultipleConsentRequestDto
{
    public int? BatchId { get; set; }

    public string RequestId { get; set; } = null!;

    public List<Consent> Consents { get; set; } = new();
}