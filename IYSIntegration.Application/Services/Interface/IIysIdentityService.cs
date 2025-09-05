using IYSIntegration.Application.Services.Models.Response.Identity;

namespace IYSIntegration.Application.Services.Interface
{
    public interface IIysIdentityService
    {
        Task<Token> GetToken(int IysCode, bool isReset);
    }
}
