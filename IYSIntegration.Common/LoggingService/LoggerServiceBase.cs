namespace IYSIntegration.Common.LoggingService;

public abstract class LoggerServiceBase
{
    protected Serilog.ILogger Logger { get; set; }

    protected LoggerServiceBase(Serilog.ILogger logger)
    {
        Logger = logger;
    }

    protected LoggerServiceBase()
    {
        Logger = null;
    }
    public void Verbose(string message) => Logger.Verbose(message);
    public void Fatal(string message) => Logger.Fatal(message);
    public void Info(string message) => Logger.Information(message);
    public void Warn(string message) => Logger.Warning(message);
    public void Debug(string message) => Logger.Debug(message);
    public void Error(string message) => Logger.Error(message);
}
