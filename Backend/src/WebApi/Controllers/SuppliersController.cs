using Application.Common;
using Application.Inventory.Commands.CreateSupplier;
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
}
