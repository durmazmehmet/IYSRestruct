using IYSIntegration.Application.Response.Identity;

namespace IYSIntegration.Application.Services.Interface
{
    public interface IIdentityService
    {
        Task<Token> GetToken(int IysCode, bool isReset);
    }
}
