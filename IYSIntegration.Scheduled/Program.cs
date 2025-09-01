using IYSIntegration.Application.Interface;
using IYSIntegration.Application.Services;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.LoggingService;
using IYSIntegration.Common.LoggingService.Loggers;
using IYSIntegration.Common.Middleware.Exceptions;
using IYSIntegration.Common.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using StackExchange.Redis;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1) Logging
        builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

        // 2) Options & Config
        builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection("CacheSettings"));

        // 3) Infrastructure singletons
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var cacheSettings = sp.GetRequiredService<IOptions<CacheSettings>>().Value;
            return ConnectionMultiplexer.Connect(cacheSettings.ConnectionString);
        });
        builder.Services.AddSingleton<ICacheService, CacheService>();

        // 4) Cross-cutting services
        builder.Services.AddSingleton<LoggerServiceBase>(_ => new GrayLogger());

        // 5) Domain/Application services
        builder.Services.AddSingleton<IDbService, DbService>();
        builder.Services.AddSingleton<IIdentityService, IdentityService>();
        builder.Services.AddSingleton<ISfIdentityService, SfIdentityService>();
        builder.Services.AddSingleton<IRestClientService, RestClientService>();
        builder.Services.AddSingleton<IConsentService, ConsentService>();
        builder.Services.AddSingleton<ISfConsentService, SfConsentService>();
        builder.Services.AddSingleton<IIntegrationService, IntegrationService>();

        // Scoped application workflows
        builder.Services.AddScoped<MultipleConsentQueryService>();
        builder.Services.AddScoped<SingleConsentAddService>();
        builder.Services.AddScoped<MultipleConsentAddService>();
        builder.Services.AddScoped<PullConsentService>();
        builder.Services.AddScoped<SfConsentScheduledService>();
        builder.Services.AddScoped<SendConsentErrorService>();

        // 6) MVC & Swagger
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseMiddleware<ExceptionMiddleware>();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}