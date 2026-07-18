using Application.Abstractions;
using Domain.Sales;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class SaleReturnRepository(AppDbContext dbContext) : ISaleReturnRepository
{
    public async Task<IReadOnlyList<SaleReturn>> GetBySaleTransactionIdAsync(int saleTransactionId, CancellationToken cancellationToken) =>
        await dbContext.SaleReturns.Include(r => r.Lines).Where(r => r.SaleTransactionId == saleTransactionId).ToListAsync(cancellationToken);

    public void Add(SaleReturn saleReturn) => dbContext.SaleReturns.Add(saleReturn);
}
