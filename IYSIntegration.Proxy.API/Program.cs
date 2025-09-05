using IYSIntegration.Application.Middleware.Exceptions;
using IYSIntegration.Application.Middleware.LoggingService;
using IYSIntegration.Application.Middleware.LoggingService.Loggers;
using IYSIntegration.Application.Services;
using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using IYSIntegration.Proxy.API.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Reflection;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Logging
        builder.Services.AddSingleton<LoggerServiceBase>(_ => new GrayLogger());

        // Configuration
        builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection("CacheSettings"));

        // Infrastructure
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var cacheSettings = sp.GetRequiredService<IOptions<CacheSettings>>().Value;
            return ConnectionMultiplexer.Connect(cacheSettings.ConnectionString);
        });

        builder.Services.AddSingleton<ICacheService, CacheService>();

        builder.Services.AddTransient<SfClient>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var url = config.GetValue<string>("SfApexUrl");
            return new SfClient(url);
        });

        // Domain services
        builder.Services.AddSingleton<IIysIdentityService, IysIdentityService>();
        builder.Services.AddSingleton<ISfIdentityService, SfIdentityService>();
        builder.Services.AddSingleton<IIysRestClientService, IysRestClientService>();
        builder.Services.AddSingleton<IDbService, DbService>();
        builder.Services.AddSingleton<IIysHelper, IysHelper>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "IYS Proxy", Version = "v1" });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);

            // Tag açıklaması
            c.DocumentFilter<TagDescriptionsDocumentFilter>();
        });

        builder.Services.AddRouting();
        builder.Services.AddMvc();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {

        }

        //app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseMiddleware<ExceptionMiddleware>();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}