using Application.Abstractions;
using Application.Identity.Commands.ResetPassword;
using Moq;

namespace Application.Tests;

public class ResetPasswordCommandHandlerTests
{
    private const string Email = "user@sarfkor.tj";
    private const string Token = "reset-token-123";
    private const string NewPassword = "NewPassword123!";

    private readonly Mock<IAuthService> _authService = new();

    private ResetPasswordCommandHandler CreateHandler() => new(_authService.Object);

    [Fact]
    public async Task Handle_ValidTokenAndEmail_ReturnsReset()
    {
        _authService.Setup(a => a.ResetPasswordAsync(Email, Token, NewPassword, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new ResetPasswordCommand(Email, Token, NewPassword), CancellationToken.None);

        Assert.Equal(ResetPasswordOutcome.Reset, result.Outcome);
    }

    [Fact]
    public async Task Handle_InvalidTokenOrUnknownEmail_ReturnsFailed()
    {
        _authService.Setup(a => a.ResetPasswordAsync(Email, Token, NewPassword, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new ResetPasswordCommand(Email, Token, NewPassword), CancellationToken.None);

        Assert.Equal(ResetPasswordOutcome.Failed, result.Outcome);
    }
}
