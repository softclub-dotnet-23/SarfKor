using System.Security.Claims;
using Application.Common;
using Application.Pricing.Commands.SubmitPriceUpdate;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class PricingController : ControllerBase
{
    [HttpPost("prices")]
    [EnableRateLimiting("contributions")]
    public async Task<IActionResult> SubmitPriceUpdate(
        SubmitPriceUpdateRequest request,
        [FromServices] ICommandHandler<SubmitPriceUpdateCommand, SubmitPriceUpdateResult> handler,
        [FromServices] IValidator<SubmitPriceUpdateCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new SubmitPriceUpdateCommand(request.ProductId, request.StoreId, userId, request.Price, request.Currency);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            SubmitPriceUpdateOutcome.Submitted => Ok(result),
            SubmitPriceUpdateOutcome.ProductNotFound => NotFound(),
            SubmitPriceUpdateOutcome.StoreNotFound => NotFound(),
            SubmitPriceUpdateOutcome.Forbidden => Forbid(),
            SubmitPriceUpdateOutcome.SubscriptionInactive => StatusCode(402, "Subscription is not active — the cabinet is closed until the store's subscription is current."),
            _ => Problem()
        };
    }
}
