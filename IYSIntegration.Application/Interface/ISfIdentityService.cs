using IYSIntegration.Common.Response.Identity;

namespace IYSIntegration.Application.Interface
{
    public interface ISfIdentityService
    {
        Task<SfToken> GetToken(bool isReset);
    }
}
