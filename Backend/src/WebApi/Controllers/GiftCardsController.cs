using System.Security.Claims;
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
    [EnableRateLimiting("money-write")]
    public async Task<IActionResult> Issue(
        IssueGiftCardRequest request,
        [FromServices] ICommandHandler<IssueGiftCardCommand, IssueGiftCardResult> handler,
        [FromServices] IValidator<IssueGiftCardCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new IssueGiftCardCommand(request.StoreId, userId, request.Amount, request.Currency, request.ExpiresAt);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            IssueGiftCardOutcome.Issued => Ok(result),
            IssueGiftCardOutcome.StoreNotFound => NotFound(),
            IssueGiftCardOutcome.Forbidden => Forbid(),
            IssueGiftCardOutcome.SubscriptionInactive => StatusCode(402, "Subscription is not active — the cabinet is closed until the store's subscription is current."),
            _ => Problem()
        };
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new RedeemGiftCardCommand(code, request.Amount, request.Currency, request.StoreId, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            RedeemGiftCardOutcome.Redeemed => Ok(result),
            RedeemGiftCardOutcome.NotFound => NotFound(),
            RedeemGiftCardOutcome.Forbidden => Forbid(),
            RedeemGiftCardOutcome.Inactive => Conflict("This gift card is inactive."),
            RedeemGiftCardOutcome.Expired => Conflict("This gift card has expired."),
            RedeemGiftCardOutcome.CurrencyMismatch => Conflict("This gift card's currency doesn't match the sale's currency."),
            RedeemGiftCardOutcome.InsufficientBalance => Conflict("Insufficient gift card balance."),
            RedeemGiftCardOutcome.SubscriptionInactive => StatusCode(402, "Subscription is not active — the cabinet is closed until the store's subscription is current."),
            _ => Problem()
        };
    }

    // Unauthenticated lookup was the original design, but it let anyone brute-force/enumerate gift
    // card codes and balances with no rate limit — closing that requires proof the caller is
    // actually a store operative, same as every other gift-card action on this controller.
    [HttpGet("{code}")]
    [Authorize("StorePartner")]
    [EnableRateLimiting("sales")]
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

        var result = await handler.Handle(query, cancellationToken);
        return result.Found ? Ok(result) : NotFound();
    }
}
