using IYSIntegration.Common.Base;
using IYSIntegration.Common.LoggingService;
using IYSIntegration.Common.Middleware.Exceptions.Handlers;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Security.Authentication;

namespace IYSIntegration.Common.Middleware.Exceptions;
public class ExceptionMiddleware
{
    private readonly HttpExceptionHandler _httpExceptionHandler = new();
    private readonly RequestDelegate next;
    private readonly LoggerServiceBase loggerService;

    public ExceptionMiddleware(RequestDelegate next, LoggerServiceBase loggerService)
    {
        this.next = next;
        this.loggerService = loggerService;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            context.Request.EnableBuffering();


            using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();

                context.Request.Body.Position = 0;

                var jsonBody = JsonConvert.DeserializeObject<dynamic>(body) ?? throw new JsonException(body);
            }

            await next(context);
        }
        catch (AuthenticationException authexp)
        {
            await LogException(context, authexp);
            await HandleExceptionAsync(context.Response, authexp);
        }
        catch (JsonException jsonException)
        {
            await LogException(context, jsonException);
            await HandleExceptionAsync(context.Response, jsonException);
        }
        catch (System.Text.Json.JsonException jsonException)
        {
            await LogException(context, jsonException);
            await HandleExceptionAsync(context.Response, jsonException);
        }
        catch (Exception exception)
        {
            await LogException(context, exception);
            await HandleExceptionAsync(context.Response, exception);
        }
    }

    private Task HandleExceptionAsync(HttpResponse response, Exception exception)
    {
        response.ContentType = "application/json";
        _httpExceptionHandler.Response = response;
        return _httpExceptionHandler.HandleExceptionAsync(exception);
    }

    private Task LogException(HttpContext context, Exception exception)
    {
        var response = new ResponseBase<string>
        {
            Data = exception.StackTrace,
            Status = ServiceResponseStatuses.Error,
        };

        response.AddMessage("Hata:", exception.Message);

        loggerService.Info(JsonConvert.SerializeObject(response));
        return Task.CompletedTask;
    }
}