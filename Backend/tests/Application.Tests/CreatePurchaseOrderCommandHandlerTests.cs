using Application.Abstractions;
using Application.Inventory.Commands.CreatePurchaseOrder;
using Domain.Inventory;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class CreatePurchaseOrderCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<IPurchaseOrderRepository> _purchaseOrderRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreatePurchaseOrderCommandHandler CreateHandler() => new(_storeRepository.Object, _storeAccessAuthorizer.Object, _purchaseOrderRepository.Object, _unitOfWork.Object);

    private static CreatePurchaseOrderCommand ValidCommand() => new(
        StoreId, SupplierId: 1, [new CreatePurchaseOrderLineInput(1, 10, 5, "TJS")], OwnerId);

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreatePurchaseOrderOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand() with { PerformedByUserId = "someone-else" }, CancellationToken.None);

        Assert.Equal(CreatePurchaseOrderOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesDraftOrderWithLines()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        PurchaseOrder? added = null;
        _purchaseOrderRepository.Setup(r => r.Add(It.IsAny<PurchaseOrder>())).Callback<PurchaseOrder>(o =>
        {
            o.Id = 1;
            added = o;
        });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreatePurchaseOrderOutcome.Created, result.Outcome);
        Assert.Equal(1, result.PurchaseOrderId);
        Assert.Equal(PurchaseOrderStatus.Draft, added!.Status);
        Assert.Single(added.Lines);
        Assert.Equal(10, added.Lines[0].Quantity);
    }
}
