using System.Security.Claims;
using Application.Common;
using Application.Pricing.Commands.RaisePriceEntryDispute;
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
            _ => Problem()
        };
    }

    [HttpPost("price-entries/{priceEntryId:int}/dispute")]
    [EnableRateLimiting("contributions")]
    public async Task<IActionResult> RaiseDispute(
        int priceEntryId,
        RaiseDisputeRequest request,
        [FromServices] ICommandHandler<RaisePriceEntryDisputeCommand, RaisePriceEntryDisputeResult> handler,
        [FromServices] IValidator<RaisePriceEntryDisputeCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new RaisePriceEntryDisputeCommand(priceEntryId, userId, request.Reason);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            RaisePriceEntryDisputeOutcome.Raised => Ok(result),
            RaisePriceEntryDisputeOutcome.PriceEntryNotFound => NotFound(),
            RaisePriceEntryDisputeOutcome.AlreadyDisputed => Conflict("This price entry already has a pending dispute."),
            _ => Problem()
        };
    }
}
