using Application.Abstractions;
using Application.Sales.Queries.GetProfitReport;
using Domain.Inventory;
using Domain.Sales;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class GetProfitReportQueryHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<ISaleTransactionRepository> _saleTransactionRepository = new();
    private readonly Mock<ICostPriceRepository> _costPriceRepository = new();

    private GetProfitReportQueryHandler CreateHandler() => new(
        _storeRepository.Object,
        _saleTransactionRepository.Object,
        _costPriceRepository.Object);

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private void SetupOwnedStore() =>
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync((Store?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetProfitReportQuery(StoreId, Today, Today, OwnerId), CancellationToken.None);

        Assert.Equal(GetProfitReportOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        SetupOwnedStore();

        var handler = CreateHandler();
        var result = await handler.Handle(new GetProfitReportQuery(StoreId, Today, Today, "someone-else"), CancellationToken.None);

        Assert.Equal(GetProfitReportOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_WithCostPrice_ComputesProfitCorrectly()
    {
        SetupOwnedStore();
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
        _costPriceRepository
            .Setup(r => r.GetLatestForStoreAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new CostPrice { ProductId = 1, StoreId = StoreId, Amount = new Money(8, "TJS"), SetByUserId = OwnerId, EffectiveFrom = DateTimeOffset.UtcNow }]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetProfitReportQuery(StoreId, Today, Today, OwnerId), CancellationToken.None);

        Assert.Equal(GetProfitReportOutcome.Found, result.Outcome);
        Assert.Equal(100m, result.Revenue);
        Assert.Equal(40m, result.TotalCost);
        Assert.Equal(60m, result.Profit);
    }

    [Fact]
    public async Task Handle_NoCostPriceSetForProduct_TreatsCostAsZero()
    {
        SetupOwnedStore();
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
        _costPriceRepository
            .Setup(r => r.GetLatestForStoreAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetProfitReportQuery(StoreId, Today, Today, OwnerId), CancellationToken.None);

        Assert.Equal(0m, result.TotalCost);
        Assert.Equal(result.Revenue, result.Profit);
    }
}
