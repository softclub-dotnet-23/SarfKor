using Application.Abstractions;
using Application.Common;

namespace Application.Identity.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandHandler(IAuthService authService) : ICommandHandler<ConfirmEmailCommand, ConfirmEmailResult>
{
    public Task<ConfirmEmailResult> Handle(ConfirmEmailCommand command, CancellationToken cancellationToken) =>
        authService.ConfirmEmailAsync(command.Email, command.Code, cancellationToken);
}
