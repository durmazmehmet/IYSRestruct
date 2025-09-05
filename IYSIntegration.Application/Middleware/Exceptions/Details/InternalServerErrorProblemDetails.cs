using IYSIntegration.Application.Services.Models.Base;

namespace IYSIntegration.Application.Middleware.Exceptions.Details;

internal class InternalServerErrorProblemDetails : ResponseBase<Exception>
{
    public InternalServerErrorProblemDetails(Exception ex)
    {
        Status = ServiceResponseStatuses.Error;
        Data = ex;
        AddMessage("Hata", ex.Message);
    }
}