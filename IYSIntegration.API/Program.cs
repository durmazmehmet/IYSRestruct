using IYSIntegration.API.Helpers;
using IYSIntegration.API.Interface;
using IYSIntegration.API.Service;
using IYSIntegration.Common.Worker;
using IYSIntegration.Common.Worker.Services;
using ApiSfConsentService = IYSIntegration.API.Service.SfConsentService;
using SfConsentWorkerService = IYSIntegration.Common.Worker.Services.SfConsentService;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.LoggingService;
using IYSIntegration.Common.LoggingService.Loggers;
using IYSIntegration.Common.Middleware.Exceptions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection("CacheSettings"));

        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var cacheSettings = sp.GetRequiredService<IOptions<CacheSettings>>().Value;
            return ConnectionMultiplexer.Connect(cacheSettings.ConnectionString);
        });

        builder.Services.AddSingleton<ICacheService, CacheService>();
        builder.Services.AddSingleton<IRestClientHelper, RestClientHelper>();
        builder.Services.AddSingleton<IIdentityService, IdentityService>();
        builder.Services.AddSingleton<IConsentService, ConsentService>();
        builder.Services.AddSingleton<IBrandService, BrandService>();
        builder.Services.AddSingleton<IRetailerService, RetailerService>();
        builder.Services.AddSingleton<IInfoService, InfoService>();
        builder.Services.AddSingleton<IRetailerAccessService, RetailerAccessService>();
        builder.Services.AddSingleton<IDbHelper, DbHelper>();
        builder.Services.AddSingleton<ISfIdentityService, SfIdentityService>();
        builder.Services.AddSingleton<ISfConsentService, ApiSfConsentService>();
        builder.Services.AddSingleton<LoggerServiceBase>(provider => { return new GrayLogger(); });
        builder.Services.AddSingleton<IIntegrationHelper, IntegrationHelper>();
        builder.Services.AddSingleton<IWorkerDbHelper, WorkerDbHelper>();
        builder.Services.AddSingleton<SingleConsentService>();
        builder.Services.AddSingleton<MultipleConsentService>();
        builder.Services.AddSingleton<PullConsentService>();
        builder.Services.AddSingleton<SfConsentWorkerService>();
        builder.Services.AddSingleton<SendConsentErrorService>();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();



        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {

        }

        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseMiddleware<ExceptionMiddleware>();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}