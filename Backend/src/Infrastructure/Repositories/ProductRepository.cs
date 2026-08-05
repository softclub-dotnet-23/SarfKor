using Application.Abstractions;
using Domain.Products;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ProductRepository(AppDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken) =>
        dbContext.Products.FirstOrDefaultAsync(p => p.Barcode.Value == barcode, cancellationToken);

    public Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken) =>
        dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

    public Task<bool> ExistsAsync(int productId, CancellationToken cancellationToken) =>
        dbContext.Products.AnyAsync(p => p.Id == productId, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken) =>
        await dbContext.Products.Where(p => productIds.Contains(p.Id)).ToListAsync(cancellationToken);

    public void Add(Product product) => dbContext.Products.Add(product);

    public async Task<IReadOnlyDictionary<int, int>> CountByBrandIdsAsync(IReadOnlyCollection<int> brandIds, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Products
            .Where(p => brandIds.Contains(p.BrandId))
            .GroupBy(p => p.BrandId)
            .Select(g => new { BrandId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.BrandId, r => r.Count);
    }

    public Task<int> ReassignBrandAsync(int fromBrandId, int toBrandId, CancellationToken cancellationToken) =>
        dbContext.Products
            .Where(p => p.BrandId == fromBrandId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.BrandId, toBrandId), cancellationToken);
}
