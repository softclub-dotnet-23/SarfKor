using Application.Catalog.Commands.CreateBrand;
using Application.Catalog.Commands.CreateCategory;
using Application.Catalog.Commands.CreateTaxRate;
using Application.Catalog.Queries.GetBrands;
using Application.Catalog.Queries.GetCategories;
using Application.Catalog.Queries.GetTaxRates;
using Application.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

// Catalog reference data (Brand/Category/TaxRate) — shared, platform-wide lookup tables that
// Product/StockMovement already reference by FK. Curated centrally (Admin) to keep the taxonomy
// consistent; all lists are public — the frontend needs them for dropdowns/browsing.
[ApiController]
[Route("api/catalog")]
public sealed class CatalogController : ControllerBase
{
    [HttpPost("brands")]
    [Authorize("Admin")]
    public async Task<IActionResult> CreateBrand(
        CreateBrandCommand command,
        [FromServices] ICommandHandler<CreateBrandCommand, CreateBrandResult> handler,
        [FromServices] IValidator<CreateBrandCommand> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("brands")]
    public async Task<IActionResult> GetBrands(
        [FromServices] IQueryHandler<GetBrandsQuery, GetBrandsResult> handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.Handle(new GetBrandsQuery(), cancellationToken));

    [HttpPost("categories")]
    [Authorize("Admin")]
    public async Task<IActionResult> CreateCategory(
        CreateCategoryCommand command,
        [FromServices] ICommandHandler<CreateCategoryCommand, CreateCategoryResult> handler,
        [FromServices] IValidator<CreateCategoryCommand> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            CreateCategoryOutcome.Created => Ok(result),
            CreateCategoryOutcome.ParentCategoryNotFound => NotFound("Parent category not found."),
            _ => Problem()
        };
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(
        [FromServices] IQueryHandler<GetCategoriesQuery, GetCategoriesResult> handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.Handle(new GetCategoriesQuery(), cancellationToken));

    [HttpPost("tax-rates")]
    [Authorize("Admin")]
    public async Task<IActionResult> CreateTaxRate(
        CreateTaxRateCommand command,
        [FromServices] ICommandHandler<CreateTaxRateCommand, CreateTaxRateResult> handler,
        [FromServices] IValidator<CreateTaxRateCommand> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("tax-rates")]
    public async Task<IActionResult> GetTaxRates(
        [FromServices] IQueryHandler<GetTaxRatesQuery, GetTaxRatesResult> handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.Handle(new GetTaxRatesQuery(), cancellationToken));
}
