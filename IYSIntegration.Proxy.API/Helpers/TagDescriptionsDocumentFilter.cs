using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IYSIntegration.Proxy.API.Helpers
{
    public class TagDescriptionsDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            swaggerDoc.Tags =
            [
                new() {
                    Name = "Consents",
                    Description = "IYS Consents Servisi"
                },
                new() {
                    Name = "Brands",
                    Description = "IYS Brands Servisi"
                },
                new() {
                    Name = "Info",
                    Description = "IYS Info Servisi"
                },
                new() {
                    Name = "Retailers Access",
                    Description = "IYS Retailers Access Servisi"
                },
                new() {
                    Name = "Retailers",
                    Description = "IYS Retailers Servisi"
                },
                new() {
                    Name = "SFProxy",
                    Description = "SF Proxy Servisi"
                }
            ];
        }
    }

}
