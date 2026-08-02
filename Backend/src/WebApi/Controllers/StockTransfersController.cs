using System.Security.Claims;
using Application.Common;
using Application.Inventory.Commands.CompleteStockTransfer;
using Application.Inventory.Commands.InitiateStockTransfer;
using Application.Inventory.Queries.GetStockTransfers;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

// Inter-store transfers — both ends must belong to the same owner (checked at use-case level).
[ApiController]
[Route("api")]
[Authorize("StorePartner")]
public sealed class StockTransfersController : ControllerBase
{
    [HttpPost("stock-transfers")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> Initiate(
        InitiateStockTransferRequest request,
        [FromServices] ICommandHandler<InitiateStockTransferCommand, InitiateStockTransferResult> handler,
        [FromServices] IValidator<InitiateStockTransferCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new InitiateStockTransferCommand(request.ProductId, request.FromStoreId, request.ToStoreId, request.Quantity, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            InitiateStockTransferOutcome.Initiated => Ok(result),
            InitiateStockTransferOutcome.FromStoreNotFound => NotFound("Source store not found."),
            InitiateStockTransferOutcome.ToStoreNotFound => NotFound("Destination store not found."),
            InitiateStockTransferOutcome.Forbidden => Forbid(),
            InitiateStockTransferOutcome.InsufficientStock => Conflict("Insufficient stock at the source store."),
            _ => Problem()
        };
    }

    [HttpPost("stock-transfers/{stockTransferId:int}/complete")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> Complete(
        int stockTransferId,
        [FromServices] ICommandHandler<CompleteStockTransferCommand, CompleteStockTransferResult> handler,
        [FromServices] IValidator<CompleteStockTransferCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new CompleteStockTransferCommand(stockTransferId, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            CompleteStockTransferOutcome.Completed => Ok(result),
            CompleteStockTransferOutcome.NotFound => NotFound(),
            CompleteStockTransferOutcome.Forbidden => Forbid(),
            CompleteStockTransferOutcome.NotInTransit => Conflict("This transfer is not in transit."),
            _ => Problem()
        };
    }

    [HttpGet("stores/{storeId:int}/stock-transfers")]
    public async Task<IActionResult> GetForStore(
        int storeId,
        [FromServices] IQueryHandler<GetStockTransfersQuery, GetStockTransfersResult> handler,
        [FromServices] IValidator<GetStockTransfersQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetStockTransfersQuery(storeId, userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome switch
        {
            GetStockTransfersOutcome.Found => Ok(result),
            GetStockTransfersOutcome.StoreNotFound => NotFound(),
            GetStockTransfersOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }
}
