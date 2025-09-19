using System.Text.Json;

namespace IYSIntegration.Application.Middleware.Exceptions.Handlers;

public abstract class ExceptionHandler
{
    public Task HandleExceptionAsync(Exception exception) =>
        exception switch
        {
            JsonException jsonException => HandleException(jsonException),
            Newtonsoft.Json.JsonException jsonException => HandleException(jsonException),
            _ => HandleException(exception)
        };

    protected abstract Task HandleException(Exception exception);
    protected abstract Task HandleException(JsonException jsonException);
    protected abstract Task HandleException(Newtonsoft.Json.JsonException jsonException);
}
