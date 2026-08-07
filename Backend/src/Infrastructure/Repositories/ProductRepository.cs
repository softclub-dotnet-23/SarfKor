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

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchAsync(
        string? search, int? categoryId, int skip, int take, CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            // Brand isn't a navigation property on Product (this codebase keeps Product/Brand
            // linked by bare FK id, see CountByBrandIdsAsync above) — resolve matching brand ids
            // first, same shape as everywhere else that joins the two.
            var matchingBrandIds = await dbContext.Brands
                .Where(b => EF.Functions.ILike(b.Name, $"%{term}%"))
                .Select(b => b.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(p =>
                EF.Functions.ILike(p.Name, $"%{term}%") ||
                EF.Functions.ILike(p.Barcode.Value, $"%{term}%") ||
                matchingBrandIds.Contains(p.BrandId));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Exact barcode match ranked first (a scanned/typed full barcode should be the top hit,
        // not wherever it lands alphabetically), then alphabetical.
        var items = await query
            .OrderByDescending(p => term != null && p.Barcode.Value == term)
            .ThenBy(p => p.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
