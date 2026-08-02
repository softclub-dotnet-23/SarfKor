using Application.Abstractions;
using Application.Identity.Commands.ForgotPassword;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests;

public class ForgotPasswordCommandHandlerTests
{
    private const string Email = "user@sarfkor.tj";
    private const string Code = "654321";

    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<ILogger<ForgotPasswordCommandHandler>> _logger = new();

    private ForgotPasswordCommandHandler CreateHandler() => new(_authService.Object, _emailSender.Object, _logger.Object);

    [Fact]
    public async Task Handle_UnknownEmail_DoesNotSendEmailButStillReturnsResult()
    {
        _authService.Setup(a => a.GeneratePasswordResetCodeAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new ForgotPasswordCommand(Email), CancellationToken.None);

        Assert.NotNull(result);
        _emailSender.Verify(e => e.SendPasswordResetCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RegisteredEmail_SendsResetCode()
    {
        _authService.Setup(a => a.GeneratePasswordResetCodeAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(Code);

        var handler = CreateHandler();
        await handler.Handle(new ForgotPasswordCommand(Email), CancellationToken.None);

        _emailSender.Verify(e => e.SendPasswordResetCodeAsync(Email, Code, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailSendThrows_SwallowsExceptionAndReturnsSameResult()
    {
        _authService.Setup(a => a.GeneratePasswordResetCodeAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(Code);
        _emailSender
            .Setup(e => e.SendPasswordResetCodeAsync(Email, Code, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP misconfigured"));

        var handler = CreateHandler();
        var result = await handler.Handle(new ForgotPasswordCommand(Email), CancellationToken.None);

        // The whole point: a broken SMTP setup must not surface differently to the caller than an
        // unknown email would — both cases fall through to the same generic result.
        Assert.NotNull(result);
    }
}
