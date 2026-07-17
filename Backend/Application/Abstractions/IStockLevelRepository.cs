using Domain.Inventory;

namespace Application.Abstractions;

public interface IStockLevelRepository
{
    /// <summary>Atomically decrements stock only if enough is available. Returns false if insufficient (no write happens).</summary>
    Task<bool> TryDecrementAsync(int productId, int storeId, int quantity, CancellationToken cancellationToken);

    /// <summary>Atomically upserts and increments stock (creates the row at the given quantity if it doesn't exist yet).</summary>
    Task IncrementAsync(int productId, int storeId, int quantity, CancellationToken cancellationToken);

    Task<IReadOnlyList<StockLevel>> GetByStoreAsync(int storeId, CancellationToken cancellationToken);
}
