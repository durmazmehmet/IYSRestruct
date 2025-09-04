using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace IYSIntegration.Application.Middleware.Exceptions.Extensions;

internal static class ProblemDetailsExtensions
{
    public static string AsJson<TProblemDetail>(this TProblemDetail details)
        where TProblemDetail : ProblemDetails => JsonConvert.SerializeObject(details);
}
