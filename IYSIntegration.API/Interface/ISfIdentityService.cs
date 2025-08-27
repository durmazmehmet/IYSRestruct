using IYSIntegration.Common.Response.Identity;

namespace IYSIntegration.API.Interface
{
    public interface ISfIdentityService
    {
        Task<SfToken> GetToken(bool isReset);
    }
}
