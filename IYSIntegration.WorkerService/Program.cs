using IYSIntegration.WorkerService.Services;
using IYSIntegration.WorkerService.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;


namespace IYSIntegration.WorkerService
{


    public class Program
    {
        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName))
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
               .ReadFrom.Configuration(Configuration)
               .CreateLogger();

            try
            {
                Log.Information($"Application Starts. Version: {System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version}");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureAppConfiguration((context, config) => { })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IDbHelper, DbHelper>();

                    services.AddSingleton<IIntegrationHelper, IntegrationHelper>();

                    services.AddHostedService<MultipleConsentQueryWorker>();

                    if (hostContext.Configuration.GetValue<bool>("RunAsSingle"))
                    {
                        services.AddHostedService<SingleConsentAddWorker>();
                    }
                    else
                    {
                        services.AddHostedService<MultipleConsentAddWorker>();
                    }

                    services.AddHostedService<PullConsentWorker>();

                    services.AddHostedService<SfConsentWorker>();

                    services.AddHostedService<SendConsentErrorWorker>();

                    services.Add(ServiceDescriptor.Singleton<IVirtualInterface>(new VirtualInterfaceClient(
                        new BasicHttpBinding
                        {
                            Security = {
                                    Mode = BasicHttpSecurityMode.Transport,
                                    Transport = new HttpTransportSecurity{ ClientCredentialType = HttpClientCredentialType.None }
                            },
                            MaxBufferPoolSize = 2147483647,
                            MaxBufferSize = 2147483647,
                            MaxReceivedMessageSize = 2147483647
                        },
                        new EndpointAddress(Configuration.GetValue<string>("IYSErrorMail:Url")))));
                })
                .UseSerilog();
    }
}
