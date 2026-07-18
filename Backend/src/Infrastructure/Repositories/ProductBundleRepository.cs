using Application.Abstractions;
using Domain.Catalog;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ProductBundleRepository(AppDbContext dbContext) : IProductBundleRepository
{
    public async Task<IReadOnlyList<ProductBundle>> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken) =>
        await dbContext.ProductBundles.Include(b => b.Items).Where(b => b.StoreId == storeId).ToListAsync(cancellationToken);

    public void Add(ProductBundle productBundle) => dbContext.ProductBundles.Add(productBundle);
}
