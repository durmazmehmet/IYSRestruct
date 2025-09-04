using IYSIntegration.API.Helpers;
using IYSIntegration.Application.Base;
using IYSIntegration.Application.Middleware.Exceptions;
using IYSIntegration.Application.Middleware.LoggingService;
using IYSIntegration.Application.Middleware.LoggingService.Loggers;
using IYSIntegration.Application.Services;
using IYSIntegration.Application.Services;
using IYSIntegration.Application.Services.Helpers;
using IYSIntegration.Application.Services.Interface;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Reflection;

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
        builder.Services.AddScoped<IIysHelper, IysHelper>();
        builder.Services.AddSingleton<IDbService, DbService>();
        builder.Services.AddSingleton<IIdentityService, IdentityService>();
        builder.Services.AddSingleton<ISfIdentityService, SfIdentityService>();
        builder.Services.AddSingleton<IRestClientService, RestClientService>();
        builder.Services.AddSingleton<IConsentService, ConsentService>();
        builder.Services.AddSingleton<ISfConsentService, SfConsentService>();
        builder.Services.AddSingleton<SalesforceClient>();
        builder.Services.AddSingleton<IysClient>();

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
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "IYS Integration", Version = "v1" });

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