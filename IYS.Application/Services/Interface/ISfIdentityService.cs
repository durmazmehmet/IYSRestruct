using IYS.Application.Services.Models.Response.Identity;

namespace IYS.Application.Services.Interface
{
    public interface ISfIdentityService
    {
        Task<SfToken> GetToken(bool isReset);
    }
}
