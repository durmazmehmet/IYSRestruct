namespace IYS.Application.Middleware.LoggingService;

public class LogDetail
{
    public string MethodName { get; set; }
    public string User { get; set; }
    public List<LogParameter> Parameters { get; set; }
    public string Stack { get; set; }

    public LogDetail()
    {
        MethodName = string.Empty;
        User = string.Empty;
        Parameters = new List<LogParameter>();
        Stack = string.Empty;
    }

    public LogDetail(string methodName, string user, List<LogParameter> parameters)
    {

        MethodName = methodName;
        User = user;
        Parameters = parameters;
    }
}
