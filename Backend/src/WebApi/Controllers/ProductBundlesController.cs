using System.Security.Claims;
using Application.Catalog.Commands.CreateProductBundle;
using Application.Catalog.Queries.GetProductBundles;
using Application.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api")]
public sealed class ProductBundlesController : ControllerBase
{
    [HttpPost("product-bundles")]
    [Authorize("StorePartner")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> Create(
        CreateProductBundleRequest request,
        [FromServices] ICommandHandler<CreateProductBundleCommand, CreateProductBundleResult> handler,
        [FromServices] IValidator<CreateProductBundleCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new CreateProductBundleCommand(request.StoreId, request.Name, request.BundlePrice, request.Currency, request.Items, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            CreateProductBundleOutcome.Created => Ok(result),
            CreateProductBundleOutcome.StoreNotFound => NotFound("Store not found."),
            CreateProductBundleOutcome.Forbidden => Forbid(),
            CreateProductBundleOutcome.ProductNotFound => NotFound("One or more products not found."),
            CreateProductBundleOutcome.SubscriptionInactive => StatusCode(402, "Subscription is not active — the cabinet is closed until the store's subscription is current."),
            _ => Problem()
        };
    }

    [HttpGet("stores/{storeId:int}/product-bundles")]
    public async Task<IActionResult> GetForStore(
        int storeId,
        [FromServices] IQueryHandler<GetProductBundlesQuery, GetProductBundlesResult> handler,
        [FromServices] IValidator<GetProductBundlesQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetProductBundlesQuery(storeId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }
}
