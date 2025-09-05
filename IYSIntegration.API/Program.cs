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
        builder.Services.AddSingleton<IDbService, DbService>();
        builder.Services.AddSingleton<SalesforceClient>();
        builder.Services.AddSingleton<ISimpleRestClient, IysClient>();

        builder.Services.AddScoped<ScheduledMultipleConsentQueryService>();
        builder.Services.AddScoped<ScheduledSingleConsentAddService>();
        builder.Services.AddScoped<ScheduledMultipleConsentAddService>();
        builder.Services.AddScoped<ScheduledPullConsentService>();
        builder.Services.AddScoped<ScheduledSfConsentService>();
        builder.Services.AddScoped<ScheduledSendConsentErrorService>();

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