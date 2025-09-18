using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Application.Services.Models.Request.Consent;
using IYSIntegration.Application.Services.Models.Response.Consent;

namespace IYSIntegration.Application.Services.Models;

public sealed class ConsentProcessingResult
{
    public int Index { get; init; }
    public required AddConsentRequest Request { get; init; }
    public ResponseBase<AddConsentResult>? ErrorResponse { get; init; }
    public bool IsValid => ErrorResponse == null;
}
