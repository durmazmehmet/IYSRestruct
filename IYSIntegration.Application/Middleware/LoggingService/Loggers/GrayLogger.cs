using Microsoft.Extensions.Configuration;
using Serilog;

namespace IYSIntegration.Application.Middleware.LoggingService.Loggers;

public class GrayLogger : LoggerServiceBase
{
    public GrayLogger()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
    }
}