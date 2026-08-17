using Application.Abstractions;
using Application.Inventory.Commands.InitiateStockTransfer;
using Domain.Inventory;
using Moq;

namespace Application.Tests;

public class InitiateStockTransferCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int FromStoreId = 1;
    private const int ToStoreId = 2;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<IStockLevelRepository> _stockLevelRepository = new();
    private readonly Mock<IStockMovementRepository> _stockMovementRepository = new();
    private readonly Mock<IStockTransferRepository> _stockTransferRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public InitiateStockTransferCommandHandlerTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));
    }

    private InitiateStockTransferCommandHandler CreateHandler() => new(
        _storeRepository.Object,
        _storeAccessAuthorizer.Object,
        _stockLevelRepository.Object,
        _stockMovementRepository.Object,
        _stockTransferRepository.Object,
        _unitOfWork.Object);

    private static InitiateStockTransferCommand ValidCommand() => new(1, FromStoreId, ToStoreId, 5, OwnerId);

    private void SetupBothStoresExist()
    {
        _storeRepository.Setup(r => r.ExistsAsync(FromStoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeRepository.Setup(r => r.ExistsAsync(ToStoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task Handle_FromStoreNotFound_ReturnsFromStoreNotFound()
    {
        _storeRepository.Setup(r => r.ExistsAsync(FromStoreId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(InitiateStockTransferOutcome.FromStoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_ToStoreOwnedBySomeoneElse_ReturnsForbidden()
    {
        SetupBothStoresExist();
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(FromStoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(ToStoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(InitiateStockTransferOutcome.Forbidden, result.Outcome);
        _stockLevelRepository.Verify(r => r.TryDecrementAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InsufficientStockAtSource_ReturnsInsufficientStockAndDoesNotCreateTransfer()
    {
        SetupBothStoresExist();
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(FromStoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(ToStoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _stockLevelRepository
            .Setup(r => r.TryDecrementAsync(1, FromStoreId, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(InitiateStockTransferOutcome.InsufficientStock, result.Outcome);
        _stockTransferRepository.Verify(r => r.Add(It.IsAny<StockTransfer>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_DecrementsSourceAndCreatesInTransitTransfer()
    {
        SetupBothStoresExist();
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(FromStoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(ToStoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _stockLevelRepository
            .Setup(r => r.TryDecrementAsync(1, FromStoreId, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _stockTransferRepository.Setup(r => r.Add(It.IsAny<StockTransfer>())).Callback<StockTransfer>(t => t.Id = 9);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(InitiateStockTransferOutcome.Initiated, result.Outcome);
        Assert.Equal(9, result.StockTransferId);
        _stockTransferRepository.Verify(r => r.Add(It.Is<StockTransfer>(t => t.Status == StockTransferStatus.InTransit)), Times.Once);
    }
}
