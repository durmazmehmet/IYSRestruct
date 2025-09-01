using IYSIntegration.Application.Interface;
using IYSIntegration.Application.Services;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.LoggingService;
using IYSIntegration.Common.LoggingService.Loggers;
using IYSIntegration.Common.Middleware.Exceptions;
using IYSIntegration.Common.Services;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
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
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis;
using System;
using System.IO;
using System.Reflection;
using IYSIntegration.API.Helpers;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1) Logging
        builder.Services.AddSingleton<LoggerServiceBase>(provider => { return new GrayLogger(); });

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

        // Scoped application workflows
        builder.Services.AddSingleton<IBrandService, BrandService>();
        builder.Services.AddSingleton<IRetailerService, RetailerService>();
        builder.Services.AddSingleton<IInfoService, InfoService>();
        builder.Services.AddSingleton<IRetailerAccessService, RetailerAccessService>();
        builder.Services.AddScoped<ScheduledMultipleConsentQueryService>();
        builder.Services.AddScoped<ScheduledSingleConsentAddService>();
        builder.Services.AddScoped<ScheduledMultipleConsentAddService>();
        builder.Services.AddScoped<ScheduledPullConsentService>();
        builder.Services.AddScoped<ScheduledSfConsentService>();
        builder.Services.AddScoped<ScheduledSendConsentErrorService>();

        // 6) MVC & Swagger
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "IYS Scheduled", Version = "v1" });

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