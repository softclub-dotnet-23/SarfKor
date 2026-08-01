using Application.Abstractions;
using Application.Sales.Queries.GetCashierAnomalyReport;
using Domain.Sales;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class GetCashierAnomalyReportQueryHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<ISaleTransactionRepository> _saleTransactionRepository = new();

    private GetCashierAnomalyReportQueryHandler CreateHandler() => new(_storeRepository.Object, _storeAccessAuthorizer.Object, _saleTransactionRepository.Object);

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private void SetupOwnedStore()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private static SaleTransaction MakeSale(string cashierUserId, SaleStatus status) => new()
    {
        StoreId = StoreId,
        CashierUserId = cashierUserId,
        IdempotencyKey = Guid.NewGuid().ToString(),
        Currency = "TJS",
        Status = status,
        CreatedAt = DateTimeOffset.UtcNow,
        Lines = [new SaleLineItem { ProductId = 1, Quantity = 1, UnitPriceAtSale = new Money(10, "TJS") }]
    };

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetCashierAnomalyReportQuery(StoreId, Today, Today, OwnerId), CancellationToken.None);

        Assert.Equal(GetCashierAnomalyReportOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        SetupOwnedStore();

        var handler = CreateHandler();
        var result = await handler.Handle(new GetCashierAnomalyReportQuery(StoreId, Today, Today, "someone-else"), CancellationToken.None);

        Assert.Equal(GetCashierAnomalyReportOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_HighVoidRateWithEnoughSamples_IsFlaggedAnomalous()
    {
        SetupOwnedStore();

        // 6 sales, 2 voided => 33% void rate, above the 20% threshold, sample size (6) >= minimum (5).
        var sales = new List<SaleTransaction>
        {
            MakeSale("cashier-1", SaleStatus.Completed),
            MakeSale("cashier-1", SaleStatus.Completed),
            MakeSale("cashier-1", SaleStatus.Completed),
            MakeSale("cashier-1", SaleStatus.Completed),
            MakeSale("cashier-1", SaleStatus.Voided),
            MakeSale("cashier-1", SaleStatus.Voided)
        };
        _saleTransactionRepository
            .Setup(r => r.GetAllInRangeAsync(StoreId, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sales);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetCashierAnomalyReportQuery(StoreId, Today, Today, OwnerId), CancellationToken.None);

        Assert.Equal(GetCashierAnomalyReportOutcome.Found, result.Outcome);
        var cashier = Assert.Single(result.Cashiers!);
        Assert.Equal(6, cashier.TotalSales);
        Assert.Equal(2, cashier.VoidedSales);
        Assert.True(cashier.IsAnomalous);
    }

    [Fact]
    public async Task Handle_HighVoidRateButTooFewSamples_IsNotFlagged()
    {
        SetupOwnedStore();

        // 1 sale, 1 voided => 100% void rate, but sample size (1) is below the minimum (5) — not statistically meaningful.
        var sales = new List<SaleTransaction> { MakeSale("cashier-1", SaleStatus.Voided) };
        _saleTransactionRepository
            .Setup(r => r.GetAllInRangeAsync(StoreId, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sales);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetCashierAnomalyReportQuery(StoreId, Today, Today, OwnerId), CancellationToken.None);

        Assert.False(result.Cashiers![0].IsAnomalous);
    }

    [Fact]
    public async Task Handle_NormalVoidRate_IsNotFlagged()
    {
        SetupOwnedStore();

        // 10 sales, 1 voided => 10% void rate, below the 20% threshold.
        var sales = Enumerable.Range(0, 9).Select(_ => MakeSale("cashier-1", SaleStatus.Completed))
            .Append(MakeSale("cashier-1", SaleStatus.Voided))
            .ToList();
        _saleTransactionRepository
            .Setup(r => r.GetAllInRangeAsync(StoreId, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sales);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetCashierAnomalyReportQuery(StoreId, Today, Today, OwnerId), CancellationToken.None);

        Assert.False(result.Cashiers![0].IsAnomalous);
    }
}
