using IYSIntegration.Application.Interface;
using IYSIntegration.Application.Services;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.LoggingService;
using IYSIntegration.Common.LoggingService.Loggers;
using IYSIntegration.Common.Middleware.Exceptions;
using IYSIntegration.Common.Services;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

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

// Domain services
builder.Services.AddSingleton<IDbService, DbService>();
builder.Services.AddSingleton<IIdentityService, IdentityService>();
builder.Services.AddSingleton<ISfIdentityService, SfIdentityService>();
builder.Services.AddSingleton<IRestClientService, RestClientService>();
builder.Services.AddSingleton<IConsentService, ConsentService>();
builder.Services.AddSingleton<ISfConsentService, SfConsentService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "IYS Integration Proxy", Version = "v1" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
