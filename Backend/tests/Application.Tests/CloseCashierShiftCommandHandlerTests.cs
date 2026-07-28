using Application.Abstractions;
using Application.Sales.Commands.CloseCashierShift;
using Domain.Sales;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class CloseCashierShiftCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;
    private const int ShiftId = 1;

    private readonly Mock<ICashierShiftRepository> _cashierShiftRepository = new();
    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreEmployeeRepository> _storeEmployeeRepository = new();
    private readonly Mock<ISaleTransactionRepository> _saleTransactionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public CloseCashierShiftCommandHandlerTests() =>
        _storeEmployeeRepository
            .Setup(r => r.IsEmployeeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

    private CloseCashierShiftCommandHandler CreateHandler() => new(
        _cashierShiftRepository.Object,
        _storeRepository.Object,
        _storeEmployeeRepository.Object,
        _saleTransactionRepository.Object,
        _unitOfWork.Object);

    private static CashierShift CreateShift(DateTimeOffset? endedAt = null) => new()
    {
        StoreId = StoreId,
        CashierUserId = OwnerId,
        OpeningCash = new Money(100, "TJS"),
        StartedAt = DateTimeOffset.UtcNow.AddHours(-4),
        EndedAt = endedAt
    };

    private void SetupOwnedStore() =>
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

    [Fact]
    public async Task Handle_ShiftNotFound_ReturnsNotFound()
    {
        _cashierShiftRepository.Setup(r => r.GetByIdAsync(ShiftId, It.IsAny<CancellationToken>())).ReturnsAsync((CashierShift?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new CloseCashierShiftCommand(ShiftId, 150, OwnerId), CancellationToken.None);

        Assert.Equal(CloseCashierShiftOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_AlreadyClosed_ReturnsAlreadyClosed()
    {
        _cashierShiftRepository.Setup(r => r.GetByIdAsync(ShiftId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateShift(DateTimeOffset.UtcNow));
        SetupOwnedStore();

        var handler = CreateHandler();
        var result = await handler.Handle(new CloseCashierShiftCommand(ShiftId, 150, OwnerId), CancellationToken.None);

        Assert.Equal(CloseCashierShiftOutcome.AlreadyClosed, result.Outcome);
    }

    [Fact]
    public async Task Handle_ExactMatch_ReturnsZeroDiscrepancy()
    {
        var shift = CreateShift();
        _cashierShiftRepository.Setup(r => r.GetByIdAsync(ShiftId, It.IsAny<CancellationToken>())).ReturnsAsync(shift);
        SetupOwnedStore();

        var sale = new SaleTransaction
        {
            StoreId = StoreId,
            CashierUserId = OwnerId,
            IdempotencyKey = "key-1",
            Currency = "TJS",
            Status = SaleStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
            Lines = [new SaleLineItem { ProductId = 1, Quantity = 2, UnitPriceAtSale = new Money(10, "TJS") }]
        };
        _saleTransactionRepository
            .Setup(r => r.GetAllInRangeAsync(StoreId, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([sale]);

        var handler = CreateHandler();
        // OpeningCash 100 + sales revenue 20 = ExpectedCash 120; cashier counts exactly 120.
        var result = await handler.Handle(new CloseCashierShiftCommand(ShiftId, 120, OwnerId), CancellationToken.None);

        Assert.Equal(CloseCashierShiftOutcome.Closed, result.Outcome);
        Assert.Equal(120, result.ExpectedCash);
        Assert.Equal(0, result.Discrepancy);
    }

    [Fact]
    public async Task Handle_ShortfallAtClose_ReportsNegativeDiscrepancy()
    {
        var shift = CreateShift();
        _cashierShiftRepository.Setup(r => r.GetByIdAsync(ShiftId, It.IsAny<CancellationToken>())).ReturnsAsync(shift);
        SetupOwnedStore();
        _saleTransactionRepository
            .Setup(r => r.GetAllInRangeAsync(StoreId, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = CreateHandler();
        // ExpectedCash = 100 (opening only, no sales); cashier only counts 90 → short by 10.
        var result = await handler.Handle(new CloseCashierShiftCommand(ShiftId, 90, OwnerId), CancellationToken.None);

        Assert.Equal(100, result.ExpectedCash);
        Assert.Equal(-10, result.Discrepancy);
        Assert.NotNull(shift.EndedAt);
    }
}
