using Application.Abstractions;
using Application.Identity.Commands.Login;
using Moq;

namespace Application.Tests;

public class LoginCommandHandlerTests
{
    private readonly Mock<IAuthService> _authService = new();

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAuthResult()
    {
        var expected = new AuthResult("user-1", "access-token", "refresh-token", DateTimeOffset.UtcNow.AddMinutes(15));
        _authService
            .Setup(a => a.LoginAsync("user@sarfkor.tj", "password123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new LoginCommandHandler(_authService.Object);
        var result = await handler.Handle(new LoginCommand("user@sarfkor.tj", "password123"), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsNull()
    {
        _authService
            .Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthResult?)null);

        var handler = new LoginCommandHandler(_authService.Object);
        var result = await handler.Handle(new LoginCommand("user@sarfkor.tj", "wrong"), CancellationToken.None);

        Assert.Null(result);
    }
}
