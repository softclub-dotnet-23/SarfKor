using Application.Assistant.Executors;
using Application.Common;
using Application.Pricing.Commands.SubmitPriceUpdate;
using Domain.Assistant;
using Moq;

namespace Application.Tests.Assistant;

public class SetPriceActionExecutorTests
{
    private readonly Mock<ICommandHandler<SubmitPriceUpdateCommand, SubmitPriceUpdateResult>> _handler = new();

    private SetPriceActionExecutor CreateExecutor() => new(_handler.Object);

    [Fact]
    public void ActionType_IsSetPrice()
    {
        Assert.Equal(AssistantActionType.SetPrice, CreateExecutor().ActionType);
    }

    [Fact]
    public async Task ExecuteAsync_ParsesParametersAndDelegatesToRealHandler_WithServerSuppliedUserIdAndStoreId()
    {
        _handler
            .Setup(h => h.Handle(It.IsAny<SubmitPriceUpdateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubmitPriceUpdateResult(SubmitPriceUpdateOutcome.Submitted, 55, DateTimeOffset.UtcNow));

        var result = await CreateExecutor().ExecuteAsync("""{"productId":7,"price":12.5,"currency":"TJS"}""", "user-1", 10, CancellationToken.None);

        Assert.True(result.Success);
        _handler.Verify(h => h.Handle(
            It.Is<SubmitPriceUpdateCommand>(c => c.ProductId == 7 && c.Price == 12.5m && c.Currency == "TJS" && c.UserId == "user-1" && c.StoreId == 10),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Forbidden_ReturnsFailure()
    {
        _handler
            .Setup(h => h.Handle(It.IsAny<SubmitPriceUpdateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubmitPriceUpdateResult(SubmitPriceUpdateOutcome.Forbidden, null, null));

        var result = await CreateExecutor().ExecuteAsync("""{"productId":7,"price":12.5,"currency":"TJS"}""", "user-1", 10, CancellationToken.None);

        Assert.False(result.Success);
    }
}
