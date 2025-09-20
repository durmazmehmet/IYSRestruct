using IYS.Application.Services.Models.Base;

namespace IYS.Application.Services.Interface
{
    public interface ISyncConsentService
    {
        Task<Consent?> SyncAsync(Consent consent, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Consent>> SyncAsync(IEnumerable<Consent> consents, CancellationToken cancellationToken = default);
    }
}
