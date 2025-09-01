using IYSIntegration.Application.Interface;
using IYSIntegration.Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.ServiceModel;

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

builder.Services.Add(ServiceDescriptor.Singleton<IVirtualInterface>(new VirtualInterfaceClient(
    new BasicHttpBinding
    {
        Security = {
            Mode = BasicHttpSecurityMode.Transport,
            Transport = new HttpTransportSecurity{ ClientCredentialType = HttpClientCredentialType.None }
        },
        MaxBufferPoolSize = 2147483647,
        MaxBufferSize = 2147483647,
        MaxReceivedMessageSize = 2147483647
    },
    new EndpointAddress(builder.Configuration.GetValue<string>("IYSErrorMail:Url"))
)));

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
