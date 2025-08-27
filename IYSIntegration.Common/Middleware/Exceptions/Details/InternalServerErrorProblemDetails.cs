using IYSIntegration.Common.Base;

namespace IYSIntegration.Common.Middleware.Exceptions.Details;

internal class InternalServerErrorProblemDetails : ResponseBase<Exception>
{
    public InternalServerErrorProblemDetails(Exception ex)
    {
        Status = ServiceResponseStatuses.Error;
        Data = ex;
        AddMessage("Hata", ex.Message);
    }
}