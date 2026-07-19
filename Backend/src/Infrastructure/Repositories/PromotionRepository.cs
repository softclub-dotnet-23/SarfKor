using Application.Abstractions;
using Domain.Offers;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PromotionRepository(AppDbContext dbContext) : IPromotionRepository
{
    public async Task<IReadOnlyList<Promotion>> GetActiveByStoreIdAsync(int storeId, DateTimeOffset asOf, CancellationToken cancellationToken) =>
        await dbContext.Promotions
            .Where(p => p.StoreId == storeId && p.StartsAt <= asOf && p.EndsAt >= asOf)
            .ToListAsync(cancellationToken);

    public void Add(Promotion promotion) => dbContext.Promotions.Add(promotion);
}
