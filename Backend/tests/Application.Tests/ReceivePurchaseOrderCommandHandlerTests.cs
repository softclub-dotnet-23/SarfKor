using Application.Abstractions;
using Application.Inventory.Commands.ReceivePurchaseOrder;
using Domain.Inventory;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class ReceivePurchaseOrderCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;
    private const int OrderId = 1;

    private readonly Mock<IPurchaseOrderRepository> _purchaseOrderRepository = new();
    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStockLevelRepository> _stockLevelRepository = new();
    private readonly Mock<IStockMovementRepository> _stockMovementRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public ReceivePurchaseOrderCommandHandlerTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });
    }

    private ReceivePurchaseOrderCommandHandler CreateHandler() => new(
        _purchaseOrderRepository.Object,
        _storeRepository.Object,
        _stockLevelRepository.Object,
        _stockMovementRepository.Object,
        _unitOfWork.Object);

    private static PurchaseOrder CreateOrder(PurchaseOrderStatus status) => new()
    {
        StoreId = StoreId,
        SupplierId = 7,
        CreatedByUserId = OwnerId,
        Status = status,
        CreatedAt = DateTimeOffset.UtcNow,
        Lines =
        [
            new PurchaseOrderLineItem { ProductId = 1, Quantity = 10, UnitCost = new Money(5, "TJS") },
            new PurchaseOrderLineItem { ProductId = 2, Quantity = 4, UnitCost = new Money(3, "TJS") }
        ]
    };

    [Fact]
    public async Task Handle_NotSubmitted_ReturnsNotSubmittedAndDoesNotTouchStock()
    {
        _purchaseOrderRepository.Setup(r => r.GetByIdAsync(OrderId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateOrder(PurchaseOrderStatus.Draft));

        var handler = CreateHandler();
        var result = await handler.Handle(new ReceivePurchaseOrderCommand(OrderId, OwnerId), CancellationToken.None);

        Assert.Equal(ReceivePurchaseOrderOutcome.NotSubmitted, result.Outcome);
        _stockLevelRepository.Verify(r => r.IncrementAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SubmittedOrder_IncrementsStockForEveryLineAndMarksReceived()
    {
        var order = CreateOrder(PurchaseOrderStatus.Submitted);
        _purchaseOrderRepository.Setup(r => r.GetByIdAsync(OrderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = CreateHandler();
        var result = await handler.Handle(new ReceivePurchaseOrderCommand(OrderId, OwnerId), CancellationToken.None);

        Assert.Equal(ReceivePurchaseOrderOutcome.Received, result.Outcome);
        Assert.Equal(PurchaseOrderStatus.Received, order.Status);
        Assert.NotNull(order.ReceivedAt);
        _stockLevelRepository.Verify(r => r.IncrementAsync(1, StoreId, 10, It.IsAny<CancellationToken>()), Times.Once);
        _stockLevelRepository.Verify(r => r.IncrementAsync(2, StoreId, 4, It.IsAny<CancellationToken>()), Times.Once);
        _stockMovementRepository.Verify(
            r => r.Add(It.Is<StockMovement>(m => m.Type == StockMovementType.Receipt && m.SupplierId == 7)),
            Times.Exactly(2));
    }
}
