using Application.Common;
using Application.Inventory.Commands.CreateSupplier;
using Application.Inventory.Commands.DeleteSupplier;
using Application.Inventory.Commands.UpdateSupplier;
using Application.Inventory.Queries.GetSuppliers;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize("StorePartner")]
public sealed class SuppliersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateSupplierCommand command,
        [FromServices] ICommandHandler<CreateSupplierCommand, CreateSupplierResult> handler,
        [FromServices] IValidator<CreateSupplierCommand> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromServices] IQueryHandler<GetSuppliersQuery, GetSuppliersResult> handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.Handle(new GetSuppliersQuery(), cancellationToken));

    [HttpPut("{supplierId:int}")]
    public async Task<IActionResult> Update(
        int supplierId,
        UpdateSupplierRequest request,
        [FromServices] ICommandHandler<UpdateSupplierCommand, UpdateSupplierResult> handler,
        [FromServices] IValidator<UpdateSupplierCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSupplierCommand(supplierId, request.Name, request.ContactPhone, request.ContactEmail);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            UpdateSupplierOutcome.Updated => Ok(result),
            UpdateSupplierOutcome.NotFound => NotFound(),
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
        var command = new DeleteSupplierCommand(supplierId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            DeleteSupplierOutcome.Deleted => Ok(result),
            DeleteSupplierOutcome.NotFound => NotFound(),
            DeleteSupplierOutcome.InUse => Conflict("This supplier is still referenced by stock movements, purchase orders, or reorder rules."),
            _ => Problem()
        };
    }
}
