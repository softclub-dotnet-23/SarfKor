using Domain.Offers;

namespace Application.Abstractions;

public interface IExpiringOfferRepository
{
    void Add(ExpiringOffer offer);
    Task<IReadOnlyList<ExpiringOffer>> GetActiveAsync(int? storeId, DateTimeOffset asOf, CancellationToken cancellationToken);
}
