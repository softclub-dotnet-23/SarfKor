using System.Security.Claims;
using Application.Common;
using Application.Inventory.Commands.CreatePurchaseOrder;
using Application.Inventory.Commands.ReceivePurchaseOrder;
using Application.Inventory.Commands.SubmitPurchaseOrder;
using Application.Inventory.Queries.GetPurchaseOrders;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api")]
[Authorize("StorePartner")]
public sealed class PurchaseOrdersController : ControllerBase
{
    [HttpPost("purchase-orders")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> Create(
        CreatePurchaseOrderRequest request,
        [FromServices] ICommandHandler<CreatePurchaseOrderCommand, CreatePurchaseOrderResult> handler,
        [FromServices] IValidator<CreatePurchaseOrderCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new CreatePurchaseOrderCommand(request.StoreId, request.SupplierId, request.Lines, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            CreatePurchaseOrderOutcome.Created => Ok(result),
            CreatePurchaseOrderOutcome.StoreNotFound => NotFound("Store not found."),
            CreatePurchaseOrderOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }

    [HttpPost("purchase-orders/{purchaseOrderId:int}/submit")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> Submit(
        int purchaseOrderId,
        [FromServices] ICommandHandler<SubmitPurchaseOrderCommand, SubmitPurchaseOrderResult> handler,
        [FromServices] IValidator<SubmitPurchaseOrderCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new SubmitPurchaseOrderCommand(purchaseOrderId, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            SubmitPurchaseOrderOutcome.Submitted => Ok(result),
            SubmitPurchaseOrderOutcome.NotFound => NotFound(),
            SubmitPurchaseOrderOutcome.Forbidden => Forbid(),
            SubmitPurchaseOrderOutcome.NotDraft => Conflict("This purchase order is no longer a draft."),
            _ => Problem()
        };
    }

    [HttpPost("purchase-orders/{purchaseOrderId:int}/receive")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> Receive(
        int purchaseOrderId,
        [FromServices] ICommandHandler<ReceivePurchaseOrderCommand, ReceivePurchaseOrderResult> handler,
        [FromServices] IValidator<ReceivePurchaseOrderCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new ReceivePurchaseOrderCommand(purchaseOrderId, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            ReceivePurchaseOrderOutcome.Received => Ok(result),
            ReceivePurchaseOrderOutcome.NotFound => NotFound(),
            ReceivePurchaseOrderOutcome.Forbidden => Forbid(),
            ReceivePurchaseOrderOutcome.NotSubmitted => Conflict("This purchase order has not been submitted."),
            _ => Problem()
        };
    }

    [HttpGet("stores/{storeId:int}/purchase-orders")]
    public async Task<IActionResult> GetForStore(
        int storeId,
        [FromServices] IQueryHandler<GetPurchaseOrdersQuery, GetPurchaseOrdersResult> handler,
        [FromServices] IValidator<GetPurchaseOrdersQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetPurchaseOrdersQuery(storeId, userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome switch
        {
            GetPurchaseOrdersOutcome.Found => Ok(result),
            GetPurchaseOrdersOutcome.StoreNotFound => NotFound(),
            GetPurchaseOrdersOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }
}
