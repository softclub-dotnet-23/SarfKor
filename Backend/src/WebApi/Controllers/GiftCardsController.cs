using Application.Common;
using Application.Payments.Commands.IssueGiftCard;
using Application.Payments.Commands.RedeemGiftCard;
using Application.Payments.Queries.GetGiftCardBalance;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api/gift-cards")]
public sealed class GiftCardsController : ControllerBase
{
    [HttpPost]
    [Authorize("StorePartner")]
    public async Task<IActionResult> Issue(
        IssueGiftCardRequest request,
        [FromServices] ICommandHandler<IssueGiftCardCommand, IssueGiftCardResult> handler,
        [FromServices] IValidator<IssueGiftCardCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new IssueGiftCardCommand(request.Amount, request.Currency, request.ExpiresAt);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(command, cancellationToken));
    }

    [HttpPost("{code}/redeem")]
    [Authorize("StorePartner")]
    [EnableRateLimiting("sales")]
    public async Task<IActionResult> Redeem(
        string code,
        RedeemGiftCardRequest request,
        [FromServices] ICommandHandler<RedeemGiftCardCommand, RedeemGiftCardResult> handler,
        [FromServices] IValidator<RedeemGiftCardCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new RedeemGiftCardCommand(code, request.Amount);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            RedeemGiftCardOutcome.Redeemed => Ok(result),
            RedeemGiftCardOutcome.NotFound => NotFound(),
            RedeemGiftCardOutcome.Inactive => Conflict("This gift card is inactive."),
            RedeemGiftCardOutcome.Expired => Conflict("This gift card has expired."),
            RedeemGiftCardOutcome.InsufficientBalance => Conflict("Insufficient gift card balance."),
            _ => Problem()
        };
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetBalance(
        string code,
        [FromServices] IQueryHandler<GetGiftCardBalanceQuery, GetGiftCardBalanceResult> handler,
        [FromServices] IValidator<GetGiftCardBalanceQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetGiftCardBalanceQuery(code);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }
}
