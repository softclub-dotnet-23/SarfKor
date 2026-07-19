using Domain.Offers;

namespace Application.Abstractions;

public interface IPromotionRepository
{
    Task<IReadOnlyList<Promotion>> GetActiveByStoreIdAsync(int storeId, DateTimeOffset asOf, CancellationToken cancellationToken);
    void Add(Promotion promotion);
}
