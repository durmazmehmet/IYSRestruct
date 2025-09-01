using IYSIntegration.Application.Middleware.Exceptions.Details;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace IYSIntegration.Application.Middleware.Exceptions.Handlers;

public class HttpExceptionHandler : ExceptionHandler
{
    public HttpResponse Response
    {
        get => _response ?? throw new ArgumentNullException(nameof(_response));
        set => _response = value;
    }

    private HttpResponse? _response;

    protected override Task HandleException(Exception exception)
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        string details = JsonConvert.SerializeObject(new InternalServerErrorProblemDetails(exception));
        return Response.WriteAsync(details);
    }

    protected override Task HandleException(System.Text.Json.JsonException jsonException)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        string details = JsonConvert.SerializeObject(new JsonExceptionProblemDetails(jsonException));
        return Response.WriteAsync(details);
    }

    protected override Task HandleException(JsonException jsonException)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        string details = JsonConvert.SerializeObject(new NewtonSoftJsonExceptionProblemDetails(jsonException));
        return Response.WriteAsync(details);
    }
}