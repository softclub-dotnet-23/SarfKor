using System.Security.Claims;
using Application.Common;
using Application.Notifications.Commands.RegisterDeviceToken;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api/device-tokens")]
[Authorize]
[EnableRateLimiting("contributions")]
public sealed class DeviceTokensController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register(
        RegisterDeviceTokenRequest request,
        [FromServices] ICommandHandler<RegisterDeviceTokenCommand, RegisterDeviceTokenResult> handler,
        [FromServices] IValidator<RegisterDeviceTokenCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new RegisterDeviceTokenCommand(userId, request.Token, request.Platform);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(command, cancellationToken));
    }
}
