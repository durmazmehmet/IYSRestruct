using System.Threading.Tasks;

namespace IYSIntegration.Application.Services.Interface
{
    public interface IOverdueOldConsentsService
    {
        Task<int> MarkOverdueAsync();
    }
}
