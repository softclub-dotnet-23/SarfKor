using Application.Abstractions;
using Application.Common;

namespace Application.Identity.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(IAuthService authService) : ICommandHandler<ChangePasswordCommand, ChangePasswordResult>
{
    public async Task<ChangePasswordResult> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await authService.ChangePasswordAsync(command.UserId, command.CurrentPassword, command.NewPassword, cancellationToken);

        if (result.UserNotFound)
            return new ChangePasswordResult(ChangePasswordOutcome.NotFound, Array.Empty<string>());

        if (result.IncorrectCurrentPassword)
            return new ChangePasswordResult(ChangePasswordOutcome.IncorrectCurrentPassword, Array.Empty<string>());

        if (!result.Succeeded)
            return new ChangePasswordResult(ChangePasswordOutcome.WeakPassword, result.Errors);

        return new ChangePasswordResult(ChangePasswordOutcome.Succeeded, Array.Empty<string>());
    }
}
