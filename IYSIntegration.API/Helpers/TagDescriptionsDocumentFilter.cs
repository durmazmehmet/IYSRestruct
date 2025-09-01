using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IYSIntegration.API.Helpers
{
    public class TagDescriptionsDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            swaggerDoc.Tags =
            [
                new() {
                    Name = "Scheduled",
                    Description = "IYS takvimli işler servisi"
                },
                new() {
                    Name = "Brand",
                    Description = "IYS Marka kodları servisi"
                },
                new() {
                    Name = "Common",
                    Description = "Ortak İşlevler"
                },
                new() {
                    Name = "Consent",
                    Description = "Consent İşlemleri için"
                },
                new() {
                    Name = "Info",
                    Description = "IYS Lokasyon bilgileri için"
                },
                new() {
                    Name = "RetailerAccess",
                    Description = "Retailer Access işlemleri servisi"
                },
                new() {
                    Name = "Retailer",
                    Description = "Retailer işlemleri servisi"
                }
            ];
        }
    }

}
