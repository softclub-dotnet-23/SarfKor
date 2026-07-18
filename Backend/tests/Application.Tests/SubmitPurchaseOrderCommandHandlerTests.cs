using Application.Abstractions;
using Application.Inventory.Commands.SubmitPurchaseOrder;
using Domain.Inventory;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class SubmitPurchaseOrderCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;
    private const int OrderId = 1;

    private readonly Mock<IPurchaseOrderRepository> _purchaseOrderRepository = new();
    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private SubmitPurchaseOrderCommandHandler CreateHandler() => new(_purchaseOrderRepository.Object, _storeRepository.Object, _unitOfWork.Object);

    private static PurchaseOrder CreateOrder(PurchaseOrderStatus status) => new()
    {
        StoreId = StoreId,
        SupplierId = 1,
        CreatedByUserId = OwnerId,
        Status = status,
        CreatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsNotFound()
    {
        _purchaseOrderRepository.Setup(r => r.GetByIdAsync(OrderId, It.IsAny<CancellationToken>())).ReturnsAsync((PurchaseOrder?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new SubmitPurchaseOrderCommand(OrderId, OwnerId), CancellationToken.None);

        Assert.Equal(SubmitPurchaseOrderOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _purchaseOrderRepository.Setup(r => r.GetByIdAsync(OrderId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateOrder(PurchaseOrderStatus.Draft));
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        var handler = CreateHandler();
        var result = await handler.Handle(new SubmitPurchaseOrderCommand(OrderId, "someone-else"), CancellationToken.None);

        Assert.Equal(SubmitPurchaseOrderOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotDraft_ReturnsNotDraft()
    {
        _purchaseOrderRepository.Setup(r => r.GetByIdAsync(OrderId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateOrder(PurchaseOrderStatus.Submitted));
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        var handler = CreateHandler();
        var result = await handler.Handle(new SubmitPurchaseOrderCommand(OrderId, OwnerId), CancellationToken.None);

        Assert.Equal(SubmitPurchaseOrderOutcome.NotDraft, result.Outcome);
    }

    [Fact]
    public async Task Handle_DraftOrder_TransitionsToSubmitted()
    {
        var order = CreateOrder(PurchaseOrderStatus.Draft);
        _purchaseOrderRepository.Setup(r => r.GetByIdAsync(OrderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        var handler = CreateHandler();
        var result = await handler.Handle(new SubmitPurchaseOrderCommand(OrderId, OwnerId), CancellationToken.None);

        Assert.Equal(SubmitPurchaseOrderOutcome.Submitted, result.Outcome);
        Assert.Equal(PurchaseOrderStatus.Submitted, order.Status);
    }
}
