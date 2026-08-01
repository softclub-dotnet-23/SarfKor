using Application.Abstractions;
using Domain.Payments;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class StoreCreditRepository(AppDbContext dbContext) : IStoreCreditRepository
{
    public Task<StoreCredit?> GetByStoreAndCustomerAsync(int storeId, int customerId, CancellationToken cancellationToken) =>
        dbContext.StoreCredits.FirstOrDefaultAsync(c => c.StoreId == storeId && c.CustomerId == customerId, cancellationToken);

    public void Add(StoreCredit storeCredit) => dbContext.StoreCredits.Add(storeCredit);

    public async Task<bool> TryDebitAsync(int storeCreditId, decimal amount, CancellationToken cancellationToken)
    {
        var rowsAffected = await dbContext.StoreCredits
            .Where(c => c.Id == storeCreditId && c.Balance.Amount >= amount)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Balance.Amount, c => c.Balance.Amount - amount)
                .SetProperty(c => c.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);

        return rowsAffected == 1;
    }

    public async Task CreditAsync(int storeCreditId, decimal amount, CancellationToken cancellationToken)
    {
        await dbContext.StoreCredits
            .Where(c => c.Id == storeCreditId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Balance.Amount, c => c.Balance.Amount + amount)
                .SetProperty(c => c.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);
    }
}
