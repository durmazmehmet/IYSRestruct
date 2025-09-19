using IYSIntegration.Application.Services.Models.Base;

namespace IYSIntegration.Application.Services.Models
{
    public class ConsentExistenceResult
    {
        public List<Consent> ExistConsents { get; } = new List<Consent>();

        public List<Consent> NonConsents { get; } = new List<Consent>();
    }
}
