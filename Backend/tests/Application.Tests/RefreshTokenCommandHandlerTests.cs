using Application.Abstractions;
using Application.Identity.Commands.RefreshToken;
using Moq;

namespace Application.Tests;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IAuthService> _authService = new();

    [Fact]
    public async Task Handle_ValidToken_ReturnsNewTokenPair()
    {
        var expected = new AuthResult("user-1", "new-access-token", "new-refresh-token", DateTimeOffset.UtcNow.AddMinutes(15));
        _authService
            .Setup(a => a.RefreshAsync("valid-refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new RefreshTokenCommandHandler(_authService.Object);
        var result = await handler.Handle(new RefreshTokenCommand("valid-refresh-token"), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_RevokedOrUnknownToken_ReturnsNull()
    {
        _authService
            .Setup(a => a.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthResult?)null);

        var handler = new RefreshTokenCommandHandler(_authService.Object);
        var result = await handler.Handle(new RefreshTokenCommand("revoked-token"), CancellationToken.None);

        Assert.Null(result);
    }
}
