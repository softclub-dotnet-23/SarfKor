using System.Security.Claims;
using Application.Common;
using Application.Notifications.Commands.CreatePriceAlert;
using Application.Notifications.Commands.DeactivatePriceAlert;
using Application.Notifications.Queries.GetPriceAlerts;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/price-alerts")]
[Authorize]
public sealed class PriceAlertsController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        CreatePriceAlertRequest request,
        [FromServices] ICommandHandler<CreatePriceAlertCommand, CreatePriceAlertResult> handler,
        [FromServices] IValidator<CreatePriceAlertCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new CreatePriceAlertCommand(userId, request.ProductId, request.TargetPrice, request.Currency);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(command, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromServices] IQueryHandler<GetPriceAlertsQuery, GetPriceAlertsResult> handler,
        [FromServices] IValidator<GetPriceAlertsQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetPriceAlertsQuery(userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }

    [HttpPost("{alertId:int}/deactivate")]
    public async Task<IActionResult> Deactivate(
        int alertId,
        [FromServices] ICommandHandler<DeactivatePriceAlertCommand, DeactivatePriceAlertResult> handler,
        [FromServices] IValidator<DeactivatePriceAlertCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new DeactivatePriceAlertCommand(alertId, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            DeactivatePriceAlertOutcome.Deactivated => Ok(result),
            DeactivatePriceAlertOutcome.NotFound => NotFound(),
            DeactivatePriceAlertOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }
}
