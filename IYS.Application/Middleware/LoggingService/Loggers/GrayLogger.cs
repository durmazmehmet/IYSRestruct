using IYS.Application.Middleware.LoggingService;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace IYS.Application.Middleware.LoggingService.Loggers;

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