using Microsoft.Extensions.Configuration;

namespace IYSIntegration.Application.Services;

public class IysProxy : SimpleRestClient
{
    public IysProxy(string baseUrl) : base(baseUrl) { }
}