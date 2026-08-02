using Application.Abstractions;
using Application.Identity.Commands.ResetPassword;
using Moq;

namespace Application.Tests;

public class ResetPasswordCommandHandlerTests
{
    private const string Email = "user@sarfkor.tj";
    private const string Code = "654321";
    private const string NewPassword = "NewPassword123!";

    private readonly Mock<IAuthService> _authService = new();

    private ResetPasswordCommandHandler CreateHandler() => new(_authService.Object);

    [Fact]
    public async Task Handle_ValidCodeAndEmail_ReturnsReset()
    {
        _authService.Setup(a => a.ResetPasswordAsync(Email, Code, NewPassword, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new ResetPasswordCommand(Email, Code, NewPassword), CancellationToken.None);

        Assert.Equal(ResetPasswordOutcome.Reset, result.Outcome);
    }

    [Fact]
    public async Task Handle_InvalidCodeOrUnknownEmail_ReturnsFailed()
    {
        _authService.Setup(a => a.ResetPasswordAsync(Email, Code, NewPassword, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new ResetPasswordCommand(Email, Code, NewPassword), CancellationToken.None);

        Assert.Equal(ResetPasswordOutcome.Failed, result.Outcome);
    }
}
