using Application.Abstractions;
using Application.Common;
using Microsoft.Extensions.Logging;

namespace Application.Identity.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    IAuthService authService,
    IEmailSender emailSender,
    ILogger<ForgotPasswordCommandHandler> logger) : ICommandHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    public async Task<ForgotPasswordResult> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var code = await authService.GeneratePasswordResetCodeAsync(command.Email, cancellationToken);

        if (code is not null)
        {
            try
            {
                await emailSender.SendPasswordResetCodeAsync(command.Email, code, cancellationToken);
            }
            catch (Exception ex)
            {
                // Swallowed on purpose: an SMTP failure must never change what the caller sees below,
                // or it would leak whether command.Email is a registered account. Still needs to be
                // observable somewhere, though, since a first-time SMTP setup is likely to be broken.
                logger.LogError(ex, "Failed to send password reset email");
            }
        }

        // Always the same result regardless of whether the email existed or the send succeeded.
        return new ForgotPasswordResult();
    }
}
