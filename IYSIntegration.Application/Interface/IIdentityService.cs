using IYSIntegration.Common.Response.Identity;

namespace IYSIntegration.Application.Interface
{
    public interface IIdentityService
    {
        Task<Token> GetToken(int IysCode, bool isReset);
    }
}
