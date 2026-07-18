using Application.Abstractions;
using Domain.Inventory;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class StockLevelRepository(AppDbContext dbContext) : IStockLevelRepository
{
    public async Task<bool> TryDecrementAsync(int productId, int storeId, int quantity, CancellationToken cancellationToken)
    {
        var rowsAffected = await dbContext.StockLevels
            .Where(s => s.ProductId == productId && s.StoreId == storeId && s.Quantity >= quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Quantity, x => x.Quantity - quantity), cancellationToken);

        return rowsAffected == 1;
    }

    public async Task IncrementAsync(int productId, int storeId, int quantity, CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "StockLevels" ("ProductId", "StoreId", "Quantity")
            VALUES ({productId}, {storeId}, {quantity})
            ON CONFLICT ("ProductId", "StoreId")
            DO UPDATE SET "Quantity" = "StockLevels"."Quantity" + {quantity}
            """,
            cancellationToken);
    }

    public async Task<IReadOnlyList<StockLevel>> GetByStoreAsync(int storeId, CancellationToken cancellationToken) =>
        await dbContext.StockLevels.Where(s => s.StoreId == storeId).ToListAsync(cancellationToken);
}
