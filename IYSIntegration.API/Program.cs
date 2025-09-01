using IYSIntegration.Application.Helpers;
using IYSIntegration.Application.Interface;
using IYSIntegration.Application.Service;
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
        builder.Services.AddSingleton<ISfConsentService, SfConsentService>();
        builder.Services.AddSingleton<LoggerServiceBase>(provider => { return new GrayLogger(); });

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