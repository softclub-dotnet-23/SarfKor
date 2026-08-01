using Application.Common;
using Application.Identity.Commands.ConfirmEmail;
using Application.Identity.Commands.ForgotPassword;
using Application.Identity.Commands.Login;
using Application.Identity.Commands.RefreshToken;
using Application.Identity.Commands.Register;
using Application.Identity.Commands.ResetPassword;
using Application.Stores.Commands.AcceptStoreEmployeeInvitation;
using Application.Stores.Commands.ConfirmStoreOwnerInvitation;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("registration")]
    public async Task<IActionResult> Register(
        RegisterCommand command,
        [FromServices] ICommandHandler<RegisterCommand, Application.Abstractions.RegisterAccountResult> handler,
        [FromServices] IValidator<RegisterCommand> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        if (result.RequiresEmailConfirmation)
            // Never include the code itself — it only ever leaves the server via the email.
            return Ok(new { requiresEmailConfirmation = true, email = command.Email });
        if (result.Auth is not null)
            return Ok(result.Auth);

        return result.EmailAlreadyRegistered
            ? Conflict("An account with this email already exists.")
            : BadRequest("Registration failed.");
    }

    [HttpPost("confirm-email")]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ConfirmEmail(
        ConfirmEmailCommand command,
        [FromServices] ICommandHandler<ConfirmEmailCommand, Application.Abstractions.ConfirmEmailResult> handler,
        [FromServices] IValidator<ConfirmEmailCommand> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        if (result.Auth is not null)
            return Ok(result.Auth);

        return result.TooManyAttempts
            ? BadRequest("Too many attempts — register again to get a new code.")
            : BadRequest("Invalid or expired code.");
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        [FromServices] ICommandHandler<LoginCommand, Application.Abstractions.LoginAccountResult> handler,
        [FromServices] IValidator<LoginCommand> validator,
        CancellationToken cancellationToken)
    {
        // IP/user agent come from the connection itself, never from the request body — a client
        // claiming a false IP would otherwise poison the SecurityEvent audit trail.
        var command = new LoginCommand(
            request.Email,
            request.Password,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.Request.Headers.UserAgent.ToString());

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        if (result.Auth is not null)
            return Ok(result.Auth);

        // 403, not 401: the password was actually correct — this tells the frontend to route the
        // caller to "enter your code" instead of "wrong password," without confirming account
        // existence to an attacker who doesn't already know the correct password.
        return result.EmailNotConfirmed
            ? StatusCode(StatusCodes.Status403Forbidden, new { requiresEmailConfirmation = true, email = request.Email })
            : Unauthorized();
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Refresh(
        RefreshTokenCommand command,
        [FromServices] ICommandHandler<RefreshTokenCommand, Application.Abstractions.AuthResult?> handler,
        [FromServices] IValidator<RefreshTokenCommand> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordCommand command,
        [FromServices] ICommandHandler<ForgotPasswordCommand, ForgotPasswordResult> handler,
        [FromServices] IValidator<ForgotPasswordCommand> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        // Always 200 regardless of outcome — the handler's generic-response guarantee only holds
        // if this action doesn't add a branch on top of it (email enumeration protection).
        await handler.Handle(command, cancellationToken);
        return Ok();
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordCommand command,
        [FromServices] ICommandHandler<ResetPasswordCommand, ResetPasswordResult> handler,
        [FromServices] IValidator<ResetPasswordCommand> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            ResetPasswordOutcome.Reset => Ok(),
            _ => BadRequest("Invalid or expired reset link.")
        };
    }

    [HttpPost("accept-invite")]
    [EnableRateLimiting("invite-accept")]
    public async Task<IActionResult> AcceptInvite(
        AcceptStoreEmployeeInvitationCommand command,
        [FromServices] ICommandHandler<AcceptStoreEmployeeInvitationCommand, AcceptStoreEmployeeInvitationResult> handler,
        [FromServices] IValidator<AcceptStoreEmployeeInvitationCommand> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            AcceptStoreEmployeeInvitationOutcome.Accepted => Ok(result),
            AcceptStoreEmployeeInvitationOutcome.AccountAlreadyExisted => Ok(result),
            AcceptStoreEmployeeInvitationOutcome.InvalidOrExpiredToken => BadRequest("Invalid or expired invitation link."),
            AcceptStoreEmployeeInvitationOutcome.RegistrationFailed => BadRequest("Could not create the account — check password requirements."),
            _ => Problem()
        };
    }

    [HttpPost("confirm-store-owner-invite")]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ConfirmStoreOwnerInvite(
        ConfirmStoreOwnerInvitationCommand command,
        [FromServices] ICommandHandler<ConfirmStoreOwnerInvitationCommand, ConfirmStoreOwnerInvitationResult> handler,
        [FromServices] IValidator<ConfirmStoreOwnerInvitationCommand> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            ConfirmStoreOwnerInvitationOutcome.Confirmed => Ok(result),
            ConfirmStoreOwnerInvitationOutcome.InvalidOrExpiredCode => BadRequest("Invalid or expired code."),
            ConfirmStoreOwnerInvitationOutcome.TooManyAttempts => BadRequest("Too many attempts — ask the administrator to resend the invitation."),
            ConfirmStoreOwnerInvitationOutcome.EmailAlreadyRegistered => Conflict("This email already has an account."),
            ConfirmStoreOwnerInvitationOutcome.RegistrationFailed => BadRequest("Could not create the account — check password requirements."),
            _ => Problem()
        };
    }
}
