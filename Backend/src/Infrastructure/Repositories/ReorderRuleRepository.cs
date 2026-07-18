using Application.Abstractions;
using Domain.Inventory;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ReorderRuleRepository(AppDbContext dbContext) : IReorderRuleRepository
{
    public async Task<IReadOnlyList<ReorderRule>> GetActiveByStoreIdAsync(int storeId, CancellationToken cancellationToken) =>
        await dbContext.ReorderRules.Where(r => r.StoreId == storeId && r.IsActive).ToListAsync(cancellationToken);

    public void Add(ReorderRule reorderRule) => dbContext.ReorderRules.Add(reorderRule);
}
