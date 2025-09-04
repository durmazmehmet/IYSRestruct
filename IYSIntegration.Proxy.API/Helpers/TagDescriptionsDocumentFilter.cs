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
                    Name = "IYSProxy",
                    Description = "IYS Proxy Servisi"
                },
                new() {
                    Name = "SFProxy",
                    Description = "SF Proxy Servisi"
                }
            ];
        }
    }

}
