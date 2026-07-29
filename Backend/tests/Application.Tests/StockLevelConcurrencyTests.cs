using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace Application.Tests;

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

    // INSERT ... RETURNING isn't a composable SELECT, so EF's SqlQuery<T> (which wraps the raw
    // SQL in an outer SELECT) rejects it -- a plain ADO.NET round-trip sidesteps that entirely.
    private static async Task<int> InsertReturningIdAsync(AppDbContext context, string sql)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        var opened = connection.State != System.Data.ConnectionState.Open;
        if (opened) await connection.OpenAsync();
        try
        {
            await using var command = new NpgsqlCommand(sql, connection);
            return (int)(await command.ExecuteScalarAsync())!;
        }
        finally
        {
            if (opened) await connection.CloseAsync();
        }
    }

    [Fact]
    public async Task TryDecrementAsync_ConcurrentCalls_NeverGoesNegative()
    {
        const int initialQuantity = 10;
        const int decrementPerCall = 3;
        const int concurrentCalls = 5;

        // FK constraints on StockLevels (Product/Store) mean the row under test needs real
        // parents — a fresh, isolated Brand/Category/User/Store/Product created and torn down
        // by this test, rather than the fabricated non-existent IDs used before FKs existed.
        var userId = Guid.NewGuid().ToString();
        int categoryId, brandId, storeId, productId;

        await using (var setupContext = CreateDbContext())
        {
            await setupContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "AspNetUsers"
                    ("Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
                     "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount")
                VALUES
                    ({userId}, {userId}, {userId}, {userId + "@concurrency-test.local"}, {userId + "@CONCURRENCY-TEST.LOCAL"},
                     false, false, false, false, 0)
                """);

            categoryId = await InsertReturningIdAsync(setupContext,
                """INSERT INTO "Categories" ("Name") VALUES ('Concurrency Test Category') RETURNING "Id" """);

            brandId = await InsertReturningIdAsync(setupContext,
                """INSERT INTO "Brands" ("Name") VALUES ('Concurrency Test Brand') RETURNING "Id" """);

            storeId = await InsertReturningIdAsync(setupContext,
                $"""
                INSERT INTO "Stores" ("OwnerUserId", "Name", "Address", "Location_Latitude", "Location_Longitude")
                VALUES ('{userId}', 'Concurrency Test Store', 'N/A', 0, 0)
                RETURNING "Id"
                """);

            productId = await InsertReturningIdAsync(setupContext,
                $"""
                INSERT INTO "Products" ("Barcode_Value", "Name", "CategoryId", "BrandId", "CountryOfOrigin", "IsSoldByWeight")
                VALUES ('0000000000000', 'Concurrency Test Product', {categoryId}, {brandId}, 'TJ', false)
                RETURNING "Id"
                """);

            await setupContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "StockLevels" ("ProductId", "StoreId", "Quantity")
                VALUES ({productId}, {storeId}, {initialQuantity})
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
            await cleanupContext.Database.ExecuteSqlInterpolatedAsync(
                $"""DELETE FROM "Products" WHERE "Id" = {productId}""");
            await cleanupContext.Database.ExecuteSqlInterpolatedAsync(
                $"""DELETE FROM "Stores" WHERE "Id" = {storeId}""");
            await cleanupContext.Database.ExecuteSqlInterpolatedAsync(
                $"""DELETE FROM "Categories" WHERE "Id" = {categoryId}""");
            await cleanupContext.Database.ExecuteSqlInterpolatedAsync(
                $"""DELETE FROM "Brands" WHERE "Id" = {brandId}""");
            await cleanupContext.Database.ExecuteSqlInterpolatedAsync(
                $"""DELETE FROM "AspNetUsers" WHERE "Id" = {userId}""");
        }
    }
}
