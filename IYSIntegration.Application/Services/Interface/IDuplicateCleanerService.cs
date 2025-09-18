using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IYSIntegration.Application.Services.Interface
{
    public interface IDuplicateCleanerService
    {
        Task CleanAsync(IEnumerable<Consent> consents);

        Task<ResponseBase<ScheduledJobStatistics>> RunBatchAsync();
    }
}
