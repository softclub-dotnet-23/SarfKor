using System.Security.Claims;
using Application.Common;
using Application.Sales.Commands.ProcessReturn;
using Application.Sales.Commands.ProcessSale;
using Application.Sales.Commands.RecordCommission;
using Application.Sales.Commands.VoidSale;
using Application.Sales.Queries.GetCommissionsForSale;
using Application.Sales.Queries.GetReturnsForSale;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize("StorePartner")]
public sealed class SalesController : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("sales")]
    public async Task<IActionResult> ProcessSale(
        ProcessSaleRequest request,
        [FromServices] ICommandHandler<ProcessSaleCommand, ProcessSaleResult> handler,
        [FromServices] IValidator<ProcessSaleCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new ProcessSaleCommand(
            request.StoreId, userId, request.IdempotencyKey, request.Currency, request.Lines,
            request.GiftCardCode, request.CustomerId, request.ApplyStoreCredit, request.BundleLines);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            ProcessSaleOutcome.Completed => Ok(result),
            ProcessSaleOutcome.StoreNotFound => NotFound("Store not found."),
            ProcessSaleOutcome.Forbidden => Forbid(),
            ProcessSaleOutcome.ProductNotFound => NotFound($"Product {result.FailedProductId} not found."),
            ProcessSaleOutcome.PriceNotFound => Conflict($"No price set for product {result.FailedProductId} at this store."),
            ProcessSaleOutcome.InsufficientStock => Conflict($"Insufficient stock for product {result.FailedProductId}."),
            ProcessSaleOutcome.GiftCardNotFound => NotFound("Gift card not found."),
            ProcessSaleOutcome.GiftCardNotUsable => Conflict("This gift card is inactive or expired."),
            ProcessSaleOutcome.CustomerNotFound => NotFound("Customer not found."),
            ProcessSaleOutcome.BundleNotFound => NotFound("Product bundle not found."),
            _ => Problem()
        };
    }

    [HttpPost("{id:int}/void")]
    public async Task<IActionResult> VoidSale(
        int id,
        VoidSaleRequest request,
        [FromServices] ICommandHandler<VoidSaleCommand, VoidSaleResult> handler,
        [FromServices] IValidator<VoidSaleCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new VoidSaleCommand(id, userId, request.Reason);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            VoidSaleOutcome.Voided => Ok(result),
            VoidSaleOutcome.NotFound => NotFound(),
            VoidSaleOutcome.Forbidden => Forbid(),
            VoidSaleOutcome.AlreadyVoided => Conflict("This sale has already been voided."),
            _ => Problem()
        };
    }

    [HttpPost("{saleTransactionId:int}/commission")]
    public async Task<IActionResult> RecordCommission(
        int saleTransactionId,
        RecordCommissionRequest request,
        [FromServices] ICommandHandler<RecordCommissionCommand, RecordCommissionResult> handler,
        [FromServices] IValidator<RecordCommissionCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new RecordCommissionCommand(saleTransactionId, request.Amount, request.Currency, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            RecordCommissionOutcome.Recorded => Ok(result),
            RecordCommissionOutcome.SaleNotFound => NotFound(),
            RecordCommissionOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }

    [HttpGet("{saleTransactionId:int}/commissions")]
    public async Task<IActionResult> GetCommissions(
        int saleTransactionId,
        [FromServices] IQueryHandler<GetCommissionsForSaleQuery, GetCommissionsForSaleResult> handler,
        [FromServices] IValidator<GetCommissionsForSaleQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetCommissionsForSaleQuery(saleTransactionId, userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome switch
        {
            GetCommissionsForSaleOutcome.Found => Ok(result),
            GetCommissionsForSaleOutcome.SaleNotFound => NotFound(),
            GetCommissionsForSaleOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }

    [HttpPost("{saleTransactionId:int}/return")]
    public async Task<IActionResult> ProcessReturn(
        int saleTransactionId,
        ProcessReturnRequest request,
        [FromServices] ICommandHandler<ProcessReturnCommand, ProcessReturnResult> handler,
        [FromServices] IValidator<ProcessReturnCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new ProcessReturnCommand(saleTransactionId, request.Lines, request.Reason, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            ProcessReturnOutcome.Processed => Ok(result),
            ProcessReturnOutcome.SaleNotFound => NotFound("Sale not found."),
            ProcessReturnOutcome.Forbidden => Forbid(),
            ProcessReturnOutcome.SaleNotCompleted => Conflict("This sale is not completed."),
            ProcessReturnOutcome.LineNotFound => NotFound("Sale line item not found."),
            ProcessReturnOutcome.ExceedsAvailableQuantity => Conflict("Return quantity exceeds what's available for this line."),
            _ => Problem()
        };
    }

    [HttpGet("{saleTransactionId:int}/returns")]
    public async Task<IActionResult> GetReturns(
        int saleTransactionId,
        [FromServices] IQueryHandler<GetReturnsForSaleQuery, GetReturnsForSaleResult> handler,
        [FromServices] IValidator<GetReturnsForSaleQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetReturnsForSaleQuery(saleTransactionId, userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome switch
        {
            GetReturnsForSaleOutcome.Found => Ok(result),
            GetReturnsForSaleOutcome.SaleNotFound => NotFound(),
            GetReturnsForSaleOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }
}
