using IYSIntegration.Application.Services.Models.Response.Identity;

namespace IYSIntegration.Application.Services.Interface
{
    public interface ISfIdentityService
    {
        Task<SfToken> GetToken(bool isReset);
    }
}
