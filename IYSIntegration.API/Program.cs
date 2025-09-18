using IYSIntegration.API.Helpers;
using IYSIntegration.Application.Middleware.Exceptions;
using IYSIntegration.Application.Middleware.LoggingService;
using IYSIntegration.Application.Middleware.LoggingService.Loggers;
using IYSIntegration.Application.Services;
using IYSIntegration.Application.Services.Interface;
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
        builder.Services.AddScoped<IysProxy>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var url = config.GetValue<string>("BaseIysProxyUrl");
            return new IysProxy(url);
        });
        builder.Services.AddScoped<ISyncConsentService, SyncConsentService>();
        builder.Services.AddScoped<AddConsentService>();
        builder.Services.AddScoped<PullConsentService>();
        builder.Services.AddScoped<SfConsentService>();
        builder.Services.AddScoped<SendConsentErrorService>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "IYS Integration", Version = "v1" });
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
            c.DocumentFilter<TagDescriptionsDocumentFilter>();
        });

        builder.Services.AddRouting();
        builder.Services.AddMvc();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {

        }

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseMiddleware<ExceptionMiddleware>();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}