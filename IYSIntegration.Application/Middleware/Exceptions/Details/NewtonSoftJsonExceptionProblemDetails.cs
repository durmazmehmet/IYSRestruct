using IYSIntegration.Application.Services.Models.Base;

namespace IYSIntegration.Application.Middleware.Exceptions.Details;

internal class NewtonSoftJsonExceptionProblemDetails : ResponseBase<Newtonsoft.Json.JsonException>
{
    public NewtonSoftJsonExceptionProblemDetails(Newtonsoft.Json.JsonException ex)
    {
        AddMessage("Hata", ex.Message);
        Status = ServiceResponseStatuses.Error;
        Data = ex;
    }
}