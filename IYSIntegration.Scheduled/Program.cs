using IYSIntegration.Application.Interface;
using IYSIntegration.Application.Services;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.LoggingService;
using IYSIntegration.Common.LoggingService.Loggers;
using IYSIntegration.Common.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection("CacheSettings"));
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var cacheSettings = sp.GetRequiredService<IOptions<CacheSettings>>().Value;
    return ConnectionMultiplexer.Connect(cacheSettings.ConnectionString);
});
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<LoggerServiceBase>(_ => new GrayLogger());
builder.Services.AddSingleton<IDbService, DbService>();
builder.Services.AddSingleton<IIdentityService, IdentityService>();
builder.Services.AddSingleton<ISfIdentityService, SfIdentityService>();
builder.Services.AddSingleton<IRestClientService, RestClientService>();
builder.Services.AddSingleton<IConsentService, ConsentService>();
builder.Services.AddSingleton<ISfConsentService, SfConsentService>();
builder.Services.AddSingleton<IIntegrationService, IntegrationService>();
builder.Services.AddScoped<MultipleConsentQueryService>();
builder.Services.AddScoped<SingleConsentAddService>();
builder.Services.AddScoped<MultipleConsentAddService>();
builder.Services.AddScoped<PullConsentService>();
builder.Services.AddScoped<SfConsentScheduledService>();
builder.Services.AddScoped<SendConsentErrorService>();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
