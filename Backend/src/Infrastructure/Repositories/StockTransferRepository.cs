using Application.Abstractions;
using Domain.Inventory;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class StockTransferRepository(AppDbContext dbContext) : IStockTransferRepository
{
    public Task<StockTransfer?> GetByIdAsync(int stockTransferId, CancellationToken cancellationToken) =>
        dbContext.StockTransfers.FirstOrDefaultAsync(t => t.Id == stockTransferId, cancellationToken);

    public async Task<IReadOnlyList<StockTransfer>> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken) =>
        await dbContext.StockTransfers.Where(t => t.FromStoreId == storeId || t.ToStoreId == storeId).ToListAsync(cancellationToken);

    public void Add(StockTransfer stockTransfer) => dbContext.StockTransfers.Add(stockTransfer);
}
