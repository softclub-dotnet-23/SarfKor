using Domain.Pricing;

namespace Application.Abstractions;

public interface IPriceEntryDisputeRepository
{
    Task<PriceEntryDispute?> GetByIdAsync(int priceEntryDisputeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PriceEntryDispute>> GetPendingAsync(CancellationToken cancellationToken);
    Task<bool> HasPendingDisputeAsync(int priceEntryId, CancellationToken cancellationToken);
    void Add(PriceEntryDispute dispute);
}
