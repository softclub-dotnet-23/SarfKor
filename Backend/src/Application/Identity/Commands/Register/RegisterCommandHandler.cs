using Application.Abstractions;
using Application.Common;
using Microsoft.Extensions.Logging;

namespace Application.Identity.Commands.Register;

public sealed class RegisterCommandHandler(
    IAuthService authService,
    IEmailSender emailSender,
    ILogger<RegisterCommandHandler> logger) : ICommandHandler<RegisterCommand, RegisterAccountResult>
{
    public async Task<RegisterAccountResult> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(command.Email, command.Password, emailPreVerified: false, cancellationToken);

        if (result.RequiresEmailConfirmation && result.EmailConfirmationCode is not null)
        {
            try
            {
                await emailSender.SendEmailConfirmationCodeAsync(command.Email, result.EmailConfirmationCode, cancellationToken);
            }
            catch (Exception ex)
            {
                // Swallowed on purpose, same reasoning as every other IEmailSender call site — a
                // broken SMTP setup must not turn "register" into a 500 for the new user. The
                // logged-email fallback in SmtpEmailSender already covers local/no-SMTP dev anyway.
                logger.LogError(ex, "Failed to send registration email-confirmation code");
            }
        }

        return result;
    }
}
