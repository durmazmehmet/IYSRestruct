using IYSIntegration.Application.Base;
using IYSIntegration.Application.Middleware.Exceptions.Handlers;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Authentication;
using System.IO;
using IYSIntegration.Application.Middleware.LoggingService;

namespace IYSIntegration.Application.Middleware.Exceptions;

public class ExceptionMiddleware
{
    private readonly HttpExceptionHandler _httpExceptionHandler = new();
    private readonly RequestDelegate _next;
    private readonly LoggerServiceBase _loggerService;

    public ExceptionMiddleware(RequestDelegate next, LoggerServiceBase loggerService)
    {
        _next = next;
        _loggerService = loggerService;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            // Sadece JSON isteklerde ve body varsa oku/doğrula
            if (ShouldValidateJson(context.Request))
            {
                context.Request.EnableBuffering();

                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (!string.IsNullOrWhiteSpace(body))
                {
                    // Geçersiz JSON'u burada 400'e yönlendirmek için JsonException fırlat
                    try
                    {
                        _ = JToken.Parse(body);
                    }
                    catch (JsonReaderException jre)
                    {
                        throw new JsonException("Invalid JSON request body.", jre);
                    }
                }
            }

            await _next(context);
        }
        catch (AuthenticationException authexp)
        {
            await LogException(context, authexp);
            await HandleExceptionAsync(context.Response, authexp);
        }
        catch (Newtonsoft.Json.JsonException jsonException)
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

    private static bool ShouldValidateJson(HttpRequest req)
    {
        if (req.ContentLength is null or 0) return false;
        var ct = req.ContentType ?? "";
        var isJson = ct.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
        var methodHasBody =
            HttpMethods.IsPost(req.Method) ||
            HttpMethods.IsPut(req.Method) ||
            HttpMethods.IsPatch(req.Method) ||
            HttpMethods.IsDelete(req.Method); // isteğe bağlı
        return isJson && methodHasBody;
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
        _loggerService.Info(JsonConvert.SerializeObject(response)); // Error kullanıyorsan burayı Error yap
        return Task.CompletedTask;
    }
}
