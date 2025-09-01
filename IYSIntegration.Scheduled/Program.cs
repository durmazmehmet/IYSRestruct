using IYSIntegration.Scheduled.Services;
using IYSIntegration.Scheduled.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.ServiceModel;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddSingleton<IDbHelper, DbHelper>();
builder.Services.AddSingleton<IIntegrationHelper, IntegrationHelper>();
builder.Services.AddScoped<MultipleConsentQueryService>();
builder.Services.AddScoped<SingleConsentAddService>();
builder.Services.AddScoped<MultipleConsentAddService>();
builder.Services.AddScoped<PullConsentService>();
builder.Services.AddScoped<SfConsentService>();
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
