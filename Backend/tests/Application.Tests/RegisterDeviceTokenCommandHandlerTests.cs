using Application.Abstractions;
using Application.Notifications.Commands.RegisterDeviceToken;
using Domain.Notifications;
using Moq;

namespace Application.Tests;

public class RegisterDeviceTokenCommandHandlerTests
{
    private readonly Mock<IDeviceTokenRepository> _deviceTokenRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RegisterDeviceTokenCommandHandler CreateHandler() => new(_deviceTokenRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_NewToken_CreatesRow()
    {
        _deviceTokenRepository.Setup(r => r.GetByTokenAsync("token-abc", It.IsAny<CancellationToken>())).ReturnsAsync((DeviceToken?)null);
        _deviceTokenRepository.Setup(r => r.Add(It.IsAny<DeviceToken>())).Callback<DeviceToken>(t => t.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(new RegisterDeviceTokenCommand("user-1", "token-abc", DevicePlatform.Android), CancellationToken.None);

        Assert.Equal(1, result.DeviceTokenId);
        _deviceTokenRepository.Verify(r => r.Add(It.IsAny<DeviceToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingToken_UpdatesInPlaceRatherThanDuplicating()
    {
        var existing = new DeviceToken { UserId = "old-user", Token = "token-abc", Platform = DevicePlatform.iOS, RegisteredAt = DateTimeOffset.UtcNow.AddDays(-30) };
        existing.Id = 5;
        _deviceTokenRepository.Setup(r => r.GetByTokenAsync("token-abc", It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var handler = CreateHandler();
        var result = await handler.Handle(new RegisterDeviceTokenCommand("new-user", "token-abc", DevicePlatform.Android), CancellationToken.None);

        Assert.Equal(5, result.DeviceTokenId);
        Assert.Equal("new-user", existing.UserId);
        Assert.Equal(DevicePlatform.Android, existing.Platform);
        _deviceTokenRepository.Verify(r => r.Add(It.IsAny<DeviceToken>()), Times.Never);
    }
}
