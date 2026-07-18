using Application.Abstractions;
using Application.Notifications.Commands.DeactivatePriceAlert;
using Domain.Notifications;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class DeactivatePriceAlertCommandHandlerTests
{
    private const string OwnerId = "user-1";
    private const int AlertId = 1;

    private readonly Mock<IPriceAlertRepository> _priceAlertRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private DeactivatePriceAlertCommandHandler CreateHandler() => new(_priceAlertRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_AlertNotFound_ReturnsNotFound()
    {
        _priceAlertRepository.Setup(r => r.GetByIdAsync(AlertId, It.IsAny<CancellationToken>())).ReturnsAsync((PriceAlert?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeactivatePriceAlertCommand(AlertId, OwnerId), CancellationToken.None);

        Assert.Equal(DeactivatePriceAlertOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _priceAlertRepository
            .Setup(r => r.GetByIdAsync(AlertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceAlert { UserId = OwnerId, ProductId = 1, TargetPrice = new Money(5, "TJS"), IsActive = true, CreatedAt = DateTimeOffset.UtcNow });

        var handler = CreateHandler();
        var result = await handler.Handle(new DeactivatePriceAlertCommand(AlertId, "someone-else"), CancellationToken.None);

        Assert.Equal(DeactivatePriceAlertOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_Owner_Deactivates()
    {
        var alert = new PriceAlert { UserId = OwnerId, ProductId = 1, TargetPrice = new Money(5, "TJS"), IsActive = true, CreatedAt = DateTimeOffset.UtcNow };
        _priceAlertRepository.Setup(r => r.GetByIdAsync(AlertId, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeactivatePriceAlertCommand(AlertId, OwnerId), CancellationToken.None);

        Assert.Equal(DeactivatePriceAlertOutcome.Deactivated, result.Outcome);
        Assert.False(alert.IsActive);
    }
}
