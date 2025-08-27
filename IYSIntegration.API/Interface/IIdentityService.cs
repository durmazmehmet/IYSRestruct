using IYSIntegration.Common.Response.Identity;

namespace IYSIntegration.API.Interface
{
    public interface IIdentityService
    {
        Task<Token> GetToken(int IysCode, bool isReset);
    }
}
