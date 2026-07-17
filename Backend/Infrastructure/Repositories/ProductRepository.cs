using Application.Abstractions;
using Domain.Products;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ProductRepository(AppDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken) =>
        dbContext.Products.FirstOrDefaultAsync(p => p.Barcode.Value == barcode, cancellationToken);

    public Task<bool> ExistsAsync(int productId, CancellationToken cancellationToken) =>
        dbContext.Products.AnyAsync(p => p.Id == productId, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken) =>
        await dbContext.Products.Where(p => productIds.Contains(p.Id)).ToListAsync(cancellationToken);

    public void Add(Product product) => dbContext.Products.Add(product);
}
