using IYS.Application.Middleware.Exceptions;
using IYS.Application.Middleware.LoggingService;
using IYS.Application.Middleware.LoggingService.Loggers;
using IYS.Application.Services;
using IYS.Application.Services.Interface;
using IYS.Application.Services.Models.Base;
using IYS.Proxy.API.Helpers;
using Microsoft.OpenApi.Models;
using System.Reflection;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddMemoryCache();
        builder.Services.Configure<Versions>(builder.Configuration.GetSection("Versions"));
        builder.Services.AddSingleton<LoggerServiceBase>(_ => new GrayLogger());
        builder.Services.AddSingleton<ICacheService, HybridCacheService>();
        builder.Services.AddSingleton<IDbService, DbService>();
        builder.Services.AddSingleton<IIysHelper, IysHelper>();
        builder.Services.AddScoped<IIysIdentityService, IysIdentityService>();
        builder.Services.AddScoped<ISfIdentityService, SalesforceIdentityService>();
        builder.Services.AddScoped<IIysRestClientService, IysRestClientService>();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "IYS Proxy", Version = "v2" });

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