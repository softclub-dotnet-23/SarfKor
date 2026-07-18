using Domain.Products;

namespace Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int productId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken);
    void Add(Product product);
}
