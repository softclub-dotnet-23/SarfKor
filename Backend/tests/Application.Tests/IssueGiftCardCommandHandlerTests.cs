using Application.Abstractions;
using Application.Payments.Commands.IssueGiftCard;
using Domain.Payments;
using Moq;

namespace Application.Tests;

public class IssueGiftCardCommandHandlerTests
{
    [Fact]
    public async Task Handle_IssuesActiveCardWithGeneratedCode()
    {
        var repo = new Mock<IGiftCardRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.Add(It.IsAny<GiftCard>())).Callback<GiftCard>(g => g.Id = 1);

        var handler = new IssueGiftCardCommandHandler(repo.Object, uow.Object);
        var result = await handler.Handle(new IssueGiftCardCommand(50, "TJS", null), CancellationToken.None);

        Assert.Equal(1, result.GiftCardId);
        Assert.False(string.IsNullOrWhiteSpace(result.Code));
        repo.Verify(r => r.Add(It.Is<GiftCard>(g => g.IsActive && g.Balance.Amount == 50)), Times.Once);
    }
}
