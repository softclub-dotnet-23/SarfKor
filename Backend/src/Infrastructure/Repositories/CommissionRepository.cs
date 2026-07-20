using Application.Abstractions;
using Domain.Sales;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class CommissionRepository(AppDbContext dbContext) : ICommissionRepository
{
    public async Task<IReadOnlyList<Commission>> GetBySaleTransactionIdAsync(int saleTransactionId, CancellationToken cancellationToken) =>
        await dbContext.Commissions.Where(c => c.SaleTransactionId == saleTransactionId).ToListAsync(cancellationToken);

    public void Add(Commission commission) => dbContext.Commissions.Add(commission);
}
