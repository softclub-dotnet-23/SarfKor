using Application.Abstractions;
using Application.Identity.Commands.ChangePassword;
using Moq;

namespace Application.Tests;

public class ChangePasswordCommandHandlerTests
{
    private const string UserId = "user-1";

    private readonly Mock<IAuthService> _authService = new();

    private ChangePasswordCommandHandler CreateHandler() => new(_authService.Object);

    [Fact]
    public async Task Handle_ServiceSucceeds_ReturnsSucceeded()
    {
        _authService
            .Setup(a => a.ChangePasswordAsync(UserId, "OldPass1!", "NewPass1!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangePasswordServiceResult(true, false, false, Array.Empty<string>()));

        var handler = CreateHandler();
        var result = await handler.Handle(new ChangePasswordCommand(UserId, "OldPass1!", "NewPass1!"), CancellationToken.None);

        Assert.Equal(ChangePasswordOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ReturnsIncorrectCurrentPassword()
    {
        _authService
            .Setup(a => a.ChangePasswordAsync(UserId, "wrong", "NewPass1!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangePasswordServiceResult(false, false, true, new List<string> { "Incorrect password." }));

        var handler = CreateHandler();
        var result = await handler.Handle(new ChangePasswordCommand(UserId, "wrong", "NewPass1!"), CancellationToken.None);

        Assert.Equal(ChangePasswordOutcome.IncorrectCurrentPassword, result.Outcome);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _authService
            .Setup(a => a.ChangePasswordAsync(UserId, "OldPass1!", "NewPass1!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangePasswordServiceResult(false, true, false, Array.Empty<string>()));

        var handler = CreateHandler();
        var result = await handler.Handle(new ChangePasswordCommand(UserId, "OldPass1!", "NewPass1!"), CancellationToken.None);

        Assert.Equal(ChangePasswordOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_WeakNewPassword_ReturnsWeakPasswordWithErrors()
    {
        _authService
            .Setup(a => a.ChangePasswordAsync(UserId, "OldPass1!", "weak", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangePasswordServiceResult(false, false, false, new List<string> { "Passwords must be at least 8 characters." }));

        var handler = CreateHandler();
        var result = await handler.Handle(new ChangePasswordCommand(UserId, "OldPass1!", "weak"), CancellationToken.None);

        Assert.Equal(ChangePasswordOutcome.WeakPassword, result.Outcome);
        Assert.Single(result.Errors);
    }
}
