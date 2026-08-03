using Application.Abstractions;
using Application.Common;

namespace Application.Identity.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IAuthService authService) : ICommandHandler<ChangePasswordCommand, ChangePasswordResult>
{
    public async Task<ChangePasswordResult> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        bool changed;
        try
        {
            changed = await authService.ChangePasswordAsync(command.UserId, command.CurrentPassword, command.NewPassword, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return new ChangePasswordResult(ChangePasswordOutcome.UserNotFound);
        }

        return new ChangePasswordResult(changed ? ChangePasswordOutcome.Changed : ChangePasswordOutcome.WrongCurrentPassword);
    }
}
