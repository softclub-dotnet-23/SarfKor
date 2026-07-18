using Application.Abstractions;
using Application.Notifications.Commands.CreatePriceAlert;
using Domain.Notifications;
using Moq;

namespace Application.Tests;

public class CreatePriceAlertCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesActiveAlert_AndReturnsItsId()
    {
        var repo = new Mock<IPriceAlertRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.Add(It.IsAny<PriceAlert>())).Callback<PriceAlert>(a => a.Id = 2);

        var handler = new CreatePriceAlertCommandHandler(repo.Object, uow.Object);
        var result = await handler.Handle(new CreatePriceAlertCommand("user-1", 5, 9.99m, "TJS"), CancellationToken.None);

        Assert.Equal(2, result.PriceAlertId);
        repo.Verify(r => r.Add(It.Is<PriceAlert>(a => a.IsActive && a.TargetPrice.Amount == 9.99m)), Times.Once);
    }
}
