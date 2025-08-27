using Microsoft.Extensions.Configuration;
using Serilog;

namespace IYSIntegration.Common.LoggingService.Loggers;

public class ConsoleLogger : LoggerServiceBase
{
    public ConsoleLogger()
    {
        var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

        Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
    }
}