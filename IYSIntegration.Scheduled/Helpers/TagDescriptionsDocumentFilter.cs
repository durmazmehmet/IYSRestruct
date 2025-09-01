using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IYSIntegration.Scheduled.Helpers
{
    public class TagDescriptionsDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            swaggerDoc.Tags =
            [
                new() {
                    Name = "Scheduled",
                    Description = "IYS takvimli işlerini yapar"
                }
            ];
        }
    }

}
