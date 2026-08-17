using System.Security.Claims;
using Application.Common;
using Application.Inventory.Commands.CreateSupplier;
using Application.Inventory.Commands.DeleteSupplier;
using Application.Inventory.Commands.UpdateSupplier;
using Application.Inventory.Queries.GetSuppliers;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

// Unlike Brand/Category/TaxRate, a supplier is a store-specific business relationship (SupplyPage
// is owner-scoped in the frontend) — every action here needs the caller to actually own/work at
// the supplier's store, checked at the Application layer, not just hold the StorePartner role.
[ApiController]
[Route("api/suppliers")]
[Authorize("StorePartner")]
[EnableRateLimiting("partner-write")]
public sealed class SuppliersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateSupplierRequest request,
        [FromServices] ICommandHandler<CreateSupplierCommand, CreateSupplierResult> handler,
        [FromServices] IValidator<CreateSupplierCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new CreateSupplierCommand(request.StoreId, userId, request.Name, request.ContactPhone, request.ContactEmail);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            CreateSupplierOutcome.Created => Ok(result),
            CreateSupplierOutcome.StoreNotFound => NotFound(),
            CreateSupplierOutcome.Forbidden => Forbid(),
            CreateSupplierOutcome.SubscriptionInactive => StatusCode(402, "Subscription is not active — the cabinet is closed until the store's subscription is current."),
            _ => Problem()
        };
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        int storeId,
        [FromServices] IQueryHandler<GetSuppliersQuery, GetSuppliersResult> handler,
        [FromServices] IValidator<GetSuppliersQuery> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var query = new GetSuppliersQuery(storeId, userId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result.Outcome switch
        {
            GetSuppliersOutcome.Found => Ok(result),
            GetSuppliersOutcome.StoreNotFound => NotFound(),
            GetSuppliersOutcome.Forbidden => Forbid(),
            _ => Problem()
        };
    }

    [HttpPut("{supplierId:int}")]
    public async Task<IActionResult> Update(
        int supplierId,
        UpdateSupplierRequest request,
        [FromServices] ICommandHandler<UpdateSupplierCommand, UpdateSupplierResult> handler,
        [FromServices] IValidator<UpdateSupplierCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new UpdateSupplierCommand(supplierId, userId, request.Name, request.ContactPhone, request.ContactEmail);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            UpdateSupplierOutcome.Updated => Ok(result),
            UpdateSupplierOutcome.NotFound => NotFound(),
            UpdateSupplierOutcome.Forbidden => Forbid(),
            UpdateSupplierOutcome.SubscriptionInactive => StatusCode(402, "Subscription is not active — the cabinet is closed until the store's subscription is current."),
            _ => Problem()
        };
    }

    [HttpDelete("{supplierId:int}")]
    public async Task<IActionResult> Delete(
        int supplierId,
        [FromServices] ICommandHandler<DeleteSupplierCommand, DeleteSupplierResult> handler,
        [FromServices] IValidator<DeleteSupplierCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new DeleteSupplierCommand(supplierId, userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            DeleteSupplierOutcome.Deleted => Ok(result),
            DeleteSupplierOutcome.NotFound => NotFound(),
            DeleteSupplierOutcome.Forbidden => Forbid(),
            DeleteSupplierOutcome.InUse => Conflict("This supplier is still referenced by stock movements, purchase orders, or reorder rules."),
            DeleteSupplierOutcome.SubscriptionInactive => StatusCode(402, "Subscription is not active — the cabinet is closed until the store's subscription is current."),
            _ => Problem()
        };
    }
}
