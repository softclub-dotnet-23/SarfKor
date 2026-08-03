using Application.Assistant.Executors;
using Application.Common;
using Application.Inventory.Commands.RecordStockReceipt;
using Domain.Assistant;
using Moq;

namespace Application.Tests.Assistant;

public class RecordStockReceiptActionExecutorTests
{
    private readonly Mock<ICommandHandler<RecordStockReceiptCommand, RecordStockReceiptResult>> _handler = new();

    private RecordStockReceiptActionExecutor CreateExecutor() => new(_handler.Object);

    [Fact]
    public void ActionType_IsRecordStockReceipt()
    {
        Assert.Equal(AssistantActionType.RecordStockReceipt, CreateExecutor().ActionType);
    }

    [Fact]
    public async Task ExecuteAsync_ParsesParametersAndDelegatesToRealHandler()
    {
        _handler
            .Setup(h => h.Handle(It.IsAny<RecordStockReceiptCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecordStockReceiptResult(RecordStockReceiptOutcome.Received, 99));

        var result = await CreateExecutor().ExecuteAsync("""{"productId":3,"quantity":25}""", "user-1", 10, CancellationToken.None);

        Assert.True(result.Success);
        _handler.Verify(h => h.Handle(
            It.Is<RecordStockReceiptCommand>(c => c.ProductId == 3 && c.Quantity == 25 && c.PerformedByUserId == "user-1" && c.StoreId == 10 && c.SupplierId == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ProductNotFound_ReturnsFailure()
    {
        _handler
            .Setup(h => h.Handle(It.IsAny<RecordStockReceiptCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecordStockReceiptResult(RecordStockReceiptOutcome.ProductNotFound, null));

        var result = await CreateExecutor().ExecuteAsync("""{"productId":3,"quantity":25}""", "user-1", 10, CancellationToken.None);

        Assert.False(result.Success);
    }
}
