using Domain.Pricing;

namespace Application.Abstractions;

public interface IPriceEntryDisputeRepository
{
    Task<PriceEntryDispute?> GetByIdAsync(int priceEntryDisputeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PriceEntryDispute>> GetPendingAsync(CancellationToken cancellationToken);
    void Add(PriceEntryDispute dispute);
}
