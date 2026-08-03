using Application.Assistant.Executors;
using Application.Common;
using Application.Offers.Commands.CreatePromotion;
using Domain.Assistant;
using Domain.Offers;
using Moq;

namespace Application.Tests.Assistant;

public class CreatePromotionActionExecutorTests
{
    private readonly Mock<ICommandHandler<CreatePromotionCommand, CreatePromotionResult>> _handler = new();

    private CreatePromotionActionExecutor CreateExecutor() => new(_handler.Object);

    [Fact]
    public void ActionType_IsCreatePromotion()
    {
        Assert.Equal(AssistantActionType.CreatePromotion, CreateExecutor().ActionType);
    }

    [Fact]
    public async Task ExecuteAsync_ParsesParametersAndDelegatesToRealHandler()
    {
        _handler
            .Setup(h => h.Handle(It.IsAny<CreatePromotionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatePromotionResult(CreatePromotionOutcome.Created, 5));

        var starts = DateTimeOffset.UtcNow;
        var ends = starts.AddDays(7);
        var parametersJson = $$"""{"productId":9,"discountType":"PercentageOff","discountValue":15,"startsAt":"{{starts:O}}","endsAt":"{{ends:O}}"}""";

        var result = await CreateExecutor().ExecuteAsync(parametersJson, "user-1", 10, CancellationToken.None);

        Assert.True(result.Success);
        _handler.Verify(h => h.Handle(
            It.Is<CreatePromotionCommand>(c =>
                c.ProductId == 9 && c.DiscountType == PromotionDiscountType.PercentageOff && c.DiscountValue == 15 &&
                c.PerformedByUserId == "user-1" && c.StoreId == 10 && c.CategoryId == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Forbidden_ReturnsFailure()
    {
        _handler
            .Setup(h => h.Handle(It.IsAny<CreatePromotionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatePromotionResult(CreatePromotionOutcome.Forbidden, null));

        var starts = DateTimeOffset.UtcNow;
        var ends = starts.AddDays(7);
        var parametersJson = $$"""{"productId":9,"discountType":"PercentageOff","discountValue":15,"startsAt":"{{starts:O}}","endsAt":"{{ends:O}}"}""";

        var result = await CreateExecutor().ExecuteAsync(parametersJson, "user-1", 10, CancellationToken.None);

        Assert.False(result.Success);
    }
}
