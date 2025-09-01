using IYSIntegration.Application.Interface;
using IYSIntegration.Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddSingleton<IDbService, DbService>();
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
