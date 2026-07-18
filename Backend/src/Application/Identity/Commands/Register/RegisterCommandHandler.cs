using Application.Abstractions;
using Application.Common;

namespace Application.Identity.Commands.Register;

public sealed class RegisterCommandHandler(IAuthService authService) : ICommandHandler<RegisterCommand, AuthResult?>
{
    public Task<AuthResult?> Handle(RegisterCommand command, CancellationToken cancellationToken) =>
        authService.RegisterAsync(command.Email, command.Password, cancellationToken);
}
