using Domain.Products;

namespace Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken);
    Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int productId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken);
    void Add(Product product);

    /// <summary>Products-per-brand — the "с числом товаров" column on the Admin brands list
    /// (ADMIN_PROMPT.md §2.8). A brand absent from the result has zero products.</summary>
    Task<IReadOnlyDictionary<int, int>> CountByBrandIdsAsync(IReadOnlyCollection<int> brandIds, CancellationToken cancellationToken);

    /// <summary>Bulk-reassigns every product from one brand to another in a single UPDATE — the
    /// transactional core of MergeBrandsCommand (ADMIN_PROMPT.md §2.8: "слияние обязано быть
    /// транзакционным"). Returns how many rows moved, for the audit log.</summary>
    Task<int> ReassignBrandAsync(int fromBrandId, int toBrandId, CancellationToken cancellationToken);
}
