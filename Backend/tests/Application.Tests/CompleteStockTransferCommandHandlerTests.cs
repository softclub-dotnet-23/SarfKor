using Application.Abstractions;
using Application.Inventory.Commands.CompleteStockTransfer;
using Domain.Inventory;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class CompleteStockTransferCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int ToStoreId = 2;
    private const int TransferId = 1;

    private readonly Mock<IStockTransferRepository> _stockTransferRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<IStockLevelRepository> _stockLevelRepository = new();
    private readonly Mock<IStockMovementRepository> _stockMovementRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public CompleteStockTransferCommandHandlerTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));
    }

    private CompleteStockTransferCommandHandler CreateHandler() => new(
        _stockTransferRepository.Object,
        _storeAccessAuthorizer.Object,
        _stockLevelRepository.Object,
        _stockMovementRepository.Object,
        _unitOfWork.Object);

    private static StockTransfer CreateTransfer(StockTransferStatus status) => new()
    {
        ProductId = 1,
        FromStoreId = 1,
        ToStoreId = ToStoreId,
        Quantity = 5,
        InitiatedByUserId = OwnerId,
        Status = status,
        CreatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task Handle_NotInTransit_ReturnsNotInTransit()
    {
        _stockTransferRepository.Setup(r => r.GetByIdAsync(TransferId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateTransfer(StockTransferStatus.Completed));
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(ToStoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new CompleteStockTransferCommand(TransferId, OwnerId), CancellationToken.None);

        Assert.Equal(CompleteStockTransferOutcome.NotInTransit, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotDestinationOwner_ReturnsForbidden()
    {
        _stockTransferRepository.Setup(r => r.GetByIdAsync(TransferId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateTransfer(StockTransferStatus.InTransit));
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(ToStoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new CompleteStockTransferCommand(TransferId, "someone-else"), CancellationToken.None);

        Assert.Equal(CompleteStockTransferOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidCommand_IncrementsDestinationAndMarksCompleted()
    {
        var transfer = CreateTransfer(StockTransferStatus.InTransit);
        _stockTransferRepository.Setup(r => r.GetByIdAsync(TransferId, It.IsAny<CancellationToken>())).ReturnsAsync(transfer);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(ToStoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new CompleteStockTransferCommand(TransferId, OwnerId), CancellationToken.None);

        Assert.Equal(CompleteStockTransferOutcome.Completed, result.Outcome);
        Assert.Equal(StockTransferStatus.Completed, transfer.Status);
        Assert.NotNull(transfer.CompletedAt);
        _stockLevelRepository.Verify(r => r.IncrementAsync(1, ToStoreId, 5, It.IsAny<CancellationToken>()), Times.Once);
    }
}
