using IYS.Application.Middleware.Exceptions;
using IYS.Application.Middleware.LoggingService;
using IYS.Application.Middleware.LoggingService.Loggers;
using IYS.Application.Services;
using IYS.Application.Services.Interface;
using IYS.Subscriber.API.Helpers;
using Microsoft.OpenApi.Models;
using System.Reflection;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton<LoggerServiceBase>(provider => { return new GrayLogger(); });
        builder.Services.AddScoped<IIysHelper, IysHelper>();
        builder.Services.AddScoped<IDbService, DbService>();
        builder.Services.AddScoped<IIysProxy>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var url = config.GetValue<string>("BaseIysProxyUrl");
            var auth = config.GetValue<string>("BaseIysProxyAuth");
            return new IysProxy(url, auth);
        });
        builder.Services.AddScoped<ErrorReportingService>();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "IYS Subscriber API", Version = "v2" });
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
            c.DocumentFilter<TagDescriptionsDocumentFilter>();
        });
        builder.Services.AddRouting();
        builder.Services.AddMvc();

        var app = builder.Build();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}