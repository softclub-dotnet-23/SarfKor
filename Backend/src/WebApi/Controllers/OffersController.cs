using System.Security.Claims;
using Application.Common;
using Application.Offers.Commands.PublishExpiringOffer;
using Application.Offers.Queries.GetExpiringOffers;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api/offers")]
public sealed class OffersController : ControllerBase
{
    [HttpPost]
    [Authorize("StorePartner")]
    public async Task<IActionResult> Publish(
        PublishExpiringOfferRequest request,
        [FromServices] ICommandHandler<PublishExpiringOfferCommand, PublishExpiringOfferResult> handler,
        [FromServices] IValidator<PublishExpiringOfferCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new PublishExpiringOfferCommand(
            request.StoreId, request.ProductId, request.OriginalPrice, request.DiscountedPrice, request.Currency, request.ExpiresAt, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            PublishExpiringOfferOutcome.Published => Ok(result),
            PublishExpiringOfferOutcome.StoreNotFound => NotFound("Store not found."),
            PublishExpiringOfferOutcome.ProductNotFound => NotFound("Product not found."),
            PublishExpiringOfferOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }

    [HttpGet("expiring")]
    [EnableRateLimiting("scan")]
    public async Task<IActionResult> GetExpiring(
        int? storeId,
        double? lat,
        double? lng,
        [FromServices] IQueryHandler<GetExpiringOffersQuery, GetExpiringOffersResult> handler,
        [FromServices] IValidator<GetExpiringOffersQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetExpiringOffersQuery(storeId, lat, lng);

        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return Ok(result);
    }
}
