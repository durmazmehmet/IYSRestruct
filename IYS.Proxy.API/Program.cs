using IYS.Application.Middleware.Exceptions;
using IYS.Application.Middleware.LoggingService;
using IYS.Application.Services;
using IYS.Application.Services.Interface;
using IYS.Application.Services.Models.Base;
using IYS.Proxy.API.Helpers;
using IYS.Application.Middleware.LoggingService.Loggers;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Reflection;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton<LoggerServiceBase>(_ => new GrayLogger());
        builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection("CacheSettings"));
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var cacheSettings = sp.GetRequiredService<IOptions<CacheSettings>>().Value;
            return ConnectionMultiplexer.Connect(cacheSettings.ConnectionString);
        });
        builder.Services.AddSingleton<ICacheService, CacheService>();
        builder.Services.AddScoped<IIysIdentityService, IysIdentityService>();
        builder.Services.AddScoped<ISfIdentityService, SalesforceIdentityService>();
        builder.Services.AddScoped<IIysHelper, IysHelper>();
        builder.Services.AddScoped<IIysRestClientService, IysRestClientService>();
        builder.Services.AddScoped<IDbService, DbService>();
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