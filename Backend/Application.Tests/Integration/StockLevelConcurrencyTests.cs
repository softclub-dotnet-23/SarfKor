using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Application.Tests.Integration;

/// <summary>
/// Требует реальный PostgreSQL (connection string из user-secrets, тот же UserSecretsId, что и у WebApi).
/// Проверяет ровно тот инвариант из CLAUDE.md §10, который невозможно подтвердить моками:
/// одновременное списание остатка никогда не уходит в отрицательное значение.
/// </summary>
[Trait("Category", "Integration")]
public class StockLevelConcurrencyTests
{
    private static AppDbContext CreateDbContext()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<StockLevelConcurrencyTests>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task TryDecrementAsync_ConcurrentCalls_NeverGoesNegative()
    {
        // Заведомо не существующие ProductId/StoreId — изоляция от прочих данных в БД.
        const int productId = 900001;
        const int storeId = 900001;
        const int initialQuantity = 10;
        const int decrementPerCall = 3;
        const int concurrentCalls = 5;

        await using (var setupContext = CreateDbContext())
        {
            await setupContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "StockLevels" ("ProductId", "StoreId", "Quantity")
                VALUES ({productId}, {storeId}, {initialQuantity})
                ON CONFLICT ("ProductId", "StoreId") DO UPDATE SET "Quantity" = {initialQuantity}
                """);
        }

        try
        {
            var tasks = Enumerable.Range(0, concurrentCalls).Select(async _ =>
            {
                await using var context = CreateDbContext();
                var repository = new StockLevelRepository(context);
                return await repository.TryDecrementAsync(productId, storeId, decrementPerCall, CancellationToken.None);
            });

            var results = await Task.WhenAll(tasks);

            await using var verifyContext = CreateDbContext();
            var finalQuantity = await verifyContext.StockLevels
                .Where(s => s.ProductId == productId && s.StoreId == storeId)
                .Select(s => s.Quantity)
                .FirstAsync();

            Assert.True(finalQuantity >= 0, $"Остаток ушёл в минус: {finalQuantity}");

            var successCount = results.Count(succeeded => succeeded);
            Assert.Equal(initialQuantity - successCount * decrementPerCall, finalQuantity);
        }
        finally
        {
            await using var cleanupContext = CreateDbContext();
            await cleanupContext.Database.ExecuteSqlInterpolatedAsync(
                $"""DELETE FROM "StockLevels" WHERE "ProductId" = {productId} AND "StoreId" = {storeId}""");
        }
    }
}
