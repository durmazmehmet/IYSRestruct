using Microsoft.Extensions.Configuration;

namespace IYSIntegration.Application.Services;

public class SfClient : SimpleRestClient
{
    public SfClient(string baseUrl) : base(baseUrl) { }
}