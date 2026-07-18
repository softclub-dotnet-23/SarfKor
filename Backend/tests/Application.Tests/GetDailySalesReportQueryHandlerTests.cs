using Application.Abstractions;
using Application.Sales.Queries.GetDailySalesReport;
using Domain.Sales;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class GetDailySalesReportQueryHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<ISaleTransactionRepository> _saleTransactionRepository = new();

    private GetDailySalesReportQueryHandler CreateHandler() => new(_storeRepository.Object, _saleTransactionRepository.Object);

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync((Store?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetDailySalesReportQuery(StoreId, Today, OwnerId), CancellationToken.None);

        Assert.Equal(GetDailySalesReportOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        var handler = CreateHandler();
        var result = await handler.Handle(new GetDailySalesReportQuery(StoreId, Today, "someone-else"), CancellationToken.None);

        Assert.Equal(GetDailySalesReportOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_Owner_ComputesCountAndRevenueFromCompletedSalesOnly()
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
            Lines = [new SaleLineItem { ProductId = 1, Quantity = 3, UnitPriceAtSale = new Money(20, "TJS") }]
        };
        _saleTransactionRepository
            .Setup(r => r.GetCompletedInRangeAsync(StoreId, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([sale]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetDailySalesReportQuery(StoreId, Today, OwnerId), CancellationToken.None);

        Assert.Equal(GetDailySalesReportOutcome.Found, result.Outcome);
        Assert.Equal(1, result.SalesCount);
        Assert.Equal(60m, result.Revenue);
        Assert.Equal("TJS", result.Currency);
    }
}
