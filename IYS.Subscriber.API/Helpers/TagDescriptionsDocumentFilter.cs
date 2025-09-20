using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IYS.Subscriber.API.Helpers
{
    public class TagDescriptionsDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            swaggerDoc.Tags =
            [
                new() {
                    Name = "Common",
                    Description = "Ortak İşlevler"
                }
            ];
        }
    }

}
