using Application.Abstractions;
using Application.Common;

namespace Application.Identity.Commands.Login;

public sealed class LoginCommandHandler(IAuthService authService) : ICommandHandler<LoginCommand, LoginAccountResult>
{
    public Task<LoginAccountResult> Handle(LoginCommand command, CancellationToken cancellationToken) =>
        authService.LoginAsync(command.Email, command.Password, command.IpAddress, command.UserAgent, cancellationToken);
}
