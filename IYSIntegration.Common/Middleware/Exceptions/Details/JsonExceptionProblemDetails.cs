using IYSIntegration.Common.Base;
using System.Text.Json;

namespace IYSIntegration.Common.Middleware.Exceptions.Details;

internal class JsonExceptionProblemDetails : ResponseBase<JsonException>
{
    public JsonExceptionProblemDetails(JsonException ex)
    {
        AddMessage("Hata", ex.Message);
        Status = ServiceResponseStatuses.Error;
        Data = ex;
    }
}
