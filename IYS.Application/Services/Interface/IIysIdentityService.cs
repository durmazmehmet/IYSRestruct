using IYS.Application.Services.Models.Response.Identity;

namespace IYS.Application.Services.Interface
{
    public interface IIysIdentityService
    {
        Task<Token> GetToken(int IysCode, bool isReset);
    }
}
