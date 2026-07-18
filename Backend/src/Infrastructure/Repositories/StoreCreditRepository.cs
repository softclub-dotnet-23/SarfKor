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
}
