namespace IYSIntegration.Common.LoggingService;
public class LogDetailWithException : LogDetail
{
    public string ExceptionMessage { get; set; }

    public LogDetailWithException() => ExceptionMessage = string.Empty;

    public LogDetailWithException(string methodName, string user, List<LogParameter> parameters, string exceptionMessage)
        : base(methodName, user, parameters) => ExceptionMessage = exceptionMessage;
}
