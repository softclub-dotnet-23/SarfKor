using Application.Abstractions;
using Domain.Inventory;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class SupplierRepository(AppDbContext dbContext) : ISupplierRepository
{
    public async Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Suppliers.ToListAsync(cancellationToken);

    public void Add(Supplier supplier) => dbContext.Suppliers.Add(supplier);
}
