using Application.Abstractions;
using Application.Identity.Commands.Register;
using Moq;

namespace Application.Tests;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IAuthService> _authService = new();

    [Fact]
    public async Task Handle_DelegatesToAuthService_AndReturnsItsResult()
    {
        var expected = new AuthResult("user-1", "access-token", "refresh-token", DateTimeOffset.UtcNow.AddMinutes(15));
        _authService
            .Setup(a => a.RegisterAsync("user@sarfkor.tj", "password123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new RegisterCommandHandler(_authService.Object);
        var result = await handler.Handle(new RegisterCommand("user@sarfkor.tj", "password123"), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_EmailAlreadyTaken_ReturnsNull()
    {
        _authService
            .Setup(a => a.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthResult?)null);

        var handler = new RegisterCommandHandler(_authService.Object);
        var result = await handler.Handle(new RegisterCommand("taken@sarfkor.tj", "password123"), CancellationToken.None);

        Assert.Null(result);
    }
}
