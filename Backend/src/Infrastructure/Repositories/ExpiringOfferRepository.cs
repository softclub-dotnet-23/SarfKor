using Application.Abstractions;
using Domain.Offers;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ExpiringOfferRepository(AppDbContext dbContext) : IExpiringOfferRepository
{
    public void Add(ExpiringOffer offer) => dbContext.ExpiringOffers.Add(offer);

    public async Task<IReadOnlyList<ExpiringOffer>> GetActiveAsync(int? storeId, DateTimeOffset asOf, CancellationToken cancellationToken) =>
        await dbContext.ExpiringOffers
            .Where(o => o.ExpiresAt > asOf && (storeId == null || o.StoreId == storeId))
            .ToListAsync(cancellationToken);
}
