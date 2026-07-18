using Application.Abstractions;
using Domain.Inventory;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PurchaseOrderRepository(AppDbContext dbContext) : IPurchaseOrderRepository
{
    public Task<PurchaseOrder?> GetByIdAsync(int purchaseOrderId, CancellationToken cancellationToken) =>
        dbContext.PurchaseOrders.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == purchaseOrderId, cancellationToken);

    public async Task<IReadOnlyList<PurchaseOrder>> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken) =>
        await dbContext.PurchaseOrders.Include(p => p.Lines).Where(p => p.StoreId == storeId).ToListAsync(cancellationToken);

    public void Add(PurchaseOrder purchaseOrder) => dbContext.PurchaseOrders.Add(purchaseOrder);
}
