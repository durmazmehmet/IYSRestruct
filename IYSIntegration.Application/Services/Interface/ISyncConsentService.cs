using IYSIntegration.Application.Services.Models.Base;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IYSIntegration.Application.Services.Interface
{
    public interface ISyncConsentService
    {
        Task<Consent?> SyncAsync(Consent consent, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Consent>> SyncAsync(IEnumerable<Consent> consents, CancellationToken cancellationToken = default);
    }
}
