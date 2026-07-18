using Application.Abstractions;
using Application.Stores.Queries.GetStoreDashboard;
using Domain.Inventory;
using Domain.Sales;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class GetStoreDashboardQueryHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<ISaleTransactionRepository> _saleTransactionRepository = new();
    private readonly Mock<IStockLevelRepository> _stockLevelRepository = new();

    private GetStoreDashboardQueryHandler CreateHandler() => new(
        _storeRepository.Object,
        _saleTransactionRepository.Object,
        _stockLevelRepository.Object);

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync((Store?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStoreDashboardQuery(StoreId, OwnerId), CancellationToken.None);

        Assert.Equal(GetStoreDashboardOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStoreDashboardQuery(StoreId, "someone-else"), CancellationToken.None);

        Assert.Equal(GetStoreDashboardOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_Owner_ReturnsTodaysSalesAndInStockProductCount()
    {
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        var sale = new SaleTransaction
        {
            StoreId = StoreId,
            CashierUserId = OwnerId,
            IdempotencyKey = "key-1",
            Currency = "TJS",
            Status = SaleStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
            Lines = [new SaleLineItem { ProductId = 1, Quantity = 5, UnitPriceAtSale = new Money(20, "TJS") }]
        };
        _saleTransactionRepository
            .Setup(r => r.GetCompletedInRangeAsync(StoreId, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([sale]);
        _stockLevelRepository
            .Setup(r => r.GetByStoreAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new StockLevel { ProductId = 1, StoreId = StoreId, Quantity = 5 },
                new StockLevel { ProductId = 2, StoreId = StoreId, Quantity = 0 }
            ]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStoreDashboardQuery(StoreId, OwnerId), CancellationToken.None);

        Assert.Equal(GetStoreDashboardOutcome.Found, result.Outcome);
        Assert.Equal(1, result.TodaySalesCount);
        Assert.Equal(100m, result.TodayRevenue);
        Assert.Equal(1, result.ProductsInStockCount);
    }
}
