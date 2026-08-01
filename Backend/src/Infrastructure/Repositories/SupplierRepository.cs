using Application.Abstractions;
using Domain.Inventory;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class SupplierRepository(AppDbContext dbContext) : ISupplierRepository
{
    public async Task<IReadOnlyList<Supplier>> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken) =>
        await dbContext.Suppliers.Where(s => s.StoreId == storeId).ToListAsync(cancellationToken);

    public Task<Supplier?> GetByIdAsync(int supplierId, CancellationToken cancellationToken) =>
        dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken);

    public async Task<bool> IsInUseAsync(int supplierId, CancellationToken cancellationToken)
    {
        if (await dbContext.StockMovements.AnyAsync(m => m.SupplierId == supplierId, cancellationToken))
            return true;
        if (await dbContext.PurchaseOrders.AnyAsync(p => p.SupplierId == supplierId, cancellationToken))
            return true;
        return await dbContext.ReorderRules.AnyAsync(r => r.PreferredSupplierId == supplierId, cancellationToken);
    }

    public void Add(Supplier supplier) => dbContext.Suppliers.Add(supplier);

    public void Remove(Supplier supplier) => dbContext.Suppliers.Remove(supplier);
}
