using Application.Common;
using Application.Identity.Commands.Login;
using Application.Identity.Commands.RefreshToken;
using Application.Identity.Commands.Register;
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
        [FromServices] ICommandHandler<RegisterCommand, Application.Abstractions.AuthResult?> handler,
        [FromServices] IValidator<RegisterCommand> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result is null ? BadRequest("Registration failed.") : Ok(result);
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        [FromServices] ICommandHandler<LoginCommand, Application.Abstractions.AuthResult?> handler,
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
        return result is null ? Unauthorized() : Ok(result);
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
}
