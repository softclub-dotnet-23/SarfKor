using Application.Abstractions;
using Application.Common;

namespace Application.Identity.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler(IAuthService authService) : ICommandHandler<ResetPasswordCommand, ResetPasswordResult>
{
    public async Task<ResetPasswordResult> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var succeeded = await authService.ResetPasswordAsync(command.Email, command.Token, command.NewPassword, cancellationToken);
        return new ResetPasswordResult(succeeded ? ResetPasswordOutcome.Reset : ResetPasswordOutcome.Failed);
    }
}
