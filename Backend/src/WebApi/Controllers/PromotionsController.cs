using System.Security.Claims;
using Application.Common;
using Application.Offers.Commands.CreatePromotion;
using Application.Offers.Queries.GetActivePromotions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api")]
public sealed class PromotionsController : ControllerBase
{
    [HttpPost("promotions")]
    [Authorize("StorePartner")]
    public async Task<IActionResult> Create(
        CreatePromotionRequest request,
        [FromServices] ICommandHandler<CreatePromotionCommand, CreatePromotionResult> handler,
        [FromServices] IValidator<CreatePromotionCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new CreatePromotionCommand(
            request.StoreId, request.ProductId, request.CategoryId,
            request.DiscountType, request.DiscountValue, request.StartsAt, request.EndsAt, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            CreatePromotionOutcome.Created => Ok(result),
            CreatePromotionOutcome.StoreNotFound => NotFound("Store not found."),
            CreatePromotionOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }

    [HttpGet("stores/{storeId:int}/promotions/active")]
    public async Task<IActionResult> GetActive(
        int storeId,
        [FromServices] IQueryHandler<GetActivePromotionsQuery, GetActivePromotionsResult> handler,
        [FromServices] IValidator<GetActivePromotionsQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetActivePromotionsQuery(storeId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }
}
