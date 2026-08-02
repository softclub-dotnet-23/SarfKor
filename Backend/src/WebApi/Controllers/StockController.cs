using System.Security.Claims;
using Application.Common;
using Application.Inventory.Commands.RecordStockReceipt;
using Application.Inventory.Commands.SetCostPrice;
using Application.Inventory.Queries.GetStockLevel;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api/stock")]
[Authorize("StorePartner")]
public sealed class StockController : ControllerBase
{
    [HttpPost("receipts")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> RecordReceipt(
        RecordStockReceiptRequest request,
        [FromServices] ICommandHandler<RecordStockReceiptCommand, RecordStockReceiptResult> handler,
        [FromServices] IValidator<RecordStockReceiptCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new RecordStockReceiptCommand(request.StoreId, request.ProductId, request.Quantity, userId, request.SupplierId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            RecordStockReceiptOutcome.Received => Ok(result),
            RecordStockReceiptOutcome.StoreNotFound => NotFound("Store not found."),
            RecordStockReceiptOutcome.ProductNotFound => NotFound("Product not found."),
            RecordStockReceiptOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }

    [HttpPost("cost-price")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> SetCostPrice(
        SetCostPriceRequest request,
        [FromServices] ICommandHandler<SetCostPriceCommand, SetCostPriceResult> handler,
        [FromServices] IValidator<SetCostPriceCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new SetCostPriceCommand(request.StoreId, request.ProductId, request.Amount, request.Currency, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            SetCostPriceOutcome.Set => Ok(result),
            SetCostPriceOutcome.StoreNotFound => NotFound("Store not found."),
            SetCostPriceOutcome.ProductNotFound => NotFound("Product not found."),
            SetCostPriceOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }

    [HttpGet]
    public async Task<IActionResult> GetStockLevel(
        int storeId,
        [FromServices] IQueryHandler<GetStockLevelQuery, GetStockLevelResult> handler,
        [FromServices] IValidator<GetStockLevelQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetStockLevelQuery(storeId, userId);

        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome switch
        {
            GetStockLevelOutcome.Found => Ok(result.Levels),
            GetStockLevelOutcome.StoreNotFound => NotFound("Store not found."),
            GetStockLevelOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }
}
