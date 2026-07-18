using Application.Abstractions;
using Application.Notifications.Queries.GetPriceAlerts;
using Domain.Notifications;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class GetPriceAlertsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsUsersAlerts()
    {
        var repo = new Mock<IPriceAlertRepository>();
        repo.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync([
            new PriceAlert { UserId = "user-1", ProductId = 5, TargetPrice = new Money(9.99m, "TJS"), IsActive = true, CreatedAt = DateTimeOffset.UtcNow }
        ]);

        var handler = new GetPriceAlertsQueryHandler(repo.Object);
        var result = await handler.Handle(new GetPriceAlertsQuery("user-1"), CancellationToken.None);

        Assert.Single(result.Alerts);
        Assert.Equal(9.99m, result.Alerts[0].TargetPrice);
        Assert.True(result.Alerts[0].IsActive);
    }
}
