using IYS.Application.Services.Models.Base;

namespace IYS.Application.Middleware.Exceptions.Details;

internal class InternalServerErrorProblemDetails : ResponseBase<Exception>
{
    public InternalServerErrorProblemDetails(Exception ex)
    {
        Status = ServiceResponseStatuses.Error;
        Data = ex;
        AddMessage("Hata", ex.Message);
    }
}