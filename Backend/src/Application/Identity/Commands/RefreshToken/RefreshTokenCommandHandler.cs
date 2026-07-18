using Application.Abstractions;
using Application.Common;

namespace Application.Identity.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(IAuthService authService) : ICommandHandler<RefreshTokenCommand, AuthResult?>
{
    public Task<AuthResult?> Handle(RefreshTokenCommand command, CancellationToken cancellationToken) =>
        authService.RefreshAsync(command.RefreshToken, cancellationToken);
}
