using Application.Abstractions;
using Application.Identity.Commands.Register;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<ILogger<RegisterCommandHandler>> _logger = new();

    private RegisterCommandHandler CreateHandler() => new(_authService.Object, _emailSender.Object, _logger.Object);

    [Fact]
    public async Task Handle_EmailPreVerifiedFalse_DelegatesToAuthService()
    {
        var expectedAuth = new AuthResult("user-1", "access-token", "refresh-token", DateTimeOffset.UtcNow.AddMinutes(15));
        var expected = new RegisterAccountResult(expectedAuth, false);
        _authService
            .Setup(a => a.RegisterAsync("user@sarfkor.tj", "password123", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = CreateHandler();
        var result = await handler.Handle(new RegisterCommand("user@sarfkor.tj", "password123"), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_EmailAlreadyTaken_ReturnsEmailAlreadyRegistered()
    {
        _authService
            .Setup(a => a.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisterAccountResult(null, true));

        var handler = CreateHandler();
        var result = await handler.Handle(new RegisterCommand("taken@sarfkor.tj", "password123"), CancellationToken.None);

        Assert.Null(result.Auth);
        Assert.True(result.EmailAlreadyRegistered);
        _emailSender.Verify(e => e.SendEmailConfirmationCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RequiresEmailConfirmation_SendsTheCodeByEmail()
    {
        _authService
            .Setup(a => a.RegisterAsync("user@sarfkor.tj", "password123", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisterAccountResult(null, false, RequiresEmailConfirmation: true, EmailConfirmationCode: "654321"));

        var handler = CreateHandler();
        var result = await handler.Handle(new RegisterCommand("user@sarfkor.tj", "password123"), CancellationToken.None);

        Assert.True(result.RequiresEmailConfirmation);
        Assert.Null(result.Auth);
        _emailSender.Verify(e => e.SendEmailConfirmationCodeAsync("user@sarfkor.tj", "654321", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailSendThrows_IsSwallowedAndStillReturnsResult()
    {
        _authService
            .Setup(a => a.RegisterAsync("user@sarfkor.tj", "password123", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisterAccountResult(null, false, RequiresEmailConfirmation: true, EmailConfirmationCode: "654321"));
        _emailSender
            .Setup(e => e.SendEmailConfirmationCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP not configured"));

        var handler = CreateHandler();
        var result = await handler.Handle(new RegisterCommand("user@sarfkor.tj", "password123"), CancellationToken.None);

        Assert.True(result.RequiresEmailConfirmation);
    }
}
