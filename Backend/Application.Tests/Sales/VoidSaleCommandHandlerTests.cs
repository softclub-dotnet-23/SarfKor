using Application.Abstractions;
using Application.Sales.Commands.VoidSale;
using Domain.Inventory;
using Domain.Sales;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;
using Xunit;

namespace Application.Tests.Sales;

public class VoidSaleCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;
    private const int SaleId = 42;

    private readonly Mock<ISaleTransactionRepository> _saleTransactionRepository = new();
    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStockLevelRepository> _stockLevelRepository = new();
    private readonly Mock<IStockMovementRepository> _stockMovementRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public VoidSaleCommandHandlerTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });
    }

    private VoidSaleCommandHandler CreateHandler() => new(
        _saleTransactionRepository.Object,
        _storeRepository.Object,
        _stockLevelRepository.Object,
        _stockMovementRepository.Object,
        _unitOfWork.Object);

    private static SaleTransaction CreateSale(SaleStatus status) => new()
    {
        StoreId = StoreId,
        CashierUserId = OwnerId,
        IdempotencyKey = "key",
        Currency = "TJS",
        Status = status,
        CreatedAt = DateTimeOffset.UtcNow,
        Lines = [new SaleLineItem { ProductId = 1, Quantity = 2, UnitPriceAtSale = new Money(10, "TJS") }]
    };

    [Fact]
    public async Task Handle_SaleNotFound_ReturnsNotFound()
    {
        _saleTransactionRepository
            .Setup(r => r.GetByIdAsync(SaleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SaleTransaction?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new VoidSaleCommand(SaleId, OwnerId, "reason"), CancellationToken.None);

        Assert.Equal(VoidSaleOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _saleTransactionRepository
            .Setup(r => r.GetByIdAsync(SaleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSale(SaleStatus.Completed));

        var handler = CreateHandler();
        var result = await handler.Handle(new VoidSaleCommand(SaleId, "someone-else", "reason"), CancellationToken.None);

        Assert.Equal(VoidSaleOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_AlreadyVoided_ReturnsAlreadyVoidedAndDoesNotTouchStock()
    {
        _saleTransactionRepository
            .Setup(r => r.GetByIdAsync(SaleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSale(SaleStatus.Voided));

        var handler = CreateHandler();
        var result = await handler.Handle(new VoidSaleCommand(SaleId, OwnerId, "reason"), CancellationToken.None);

        Assert.Equal(VoidSaleOutcome.AlreadyVoided, result.Outcome);
        _stockLevelRepository.Verify(
            r => r.IncrementAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ValidVoid_RestoresStockAndReturnsVoided()
    {
        _saleTransactionRepository
            .Setup(r => r.GetByIdAsync(SaleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSale(SaleStatus.Completed));

        var handler = CreateHandler();
        var result = await handler.Handle(new VoidSaleCommand(SaleId, OwnerId, "damaged"), CancellationToken.None);

        Assert.Equal(VoidSaleOutcome.Voided, result.Outcome);
        _stockLevelRepository.Verify(r => r.IncrementAsync(1, StoreId, 2, It.IsAny<CancellationToken>()), Times.Once);
        _stockMovementRepository.Verify(r => r.Add(It.IsAny<StockMovement>()), Times.Once);
    }
}
