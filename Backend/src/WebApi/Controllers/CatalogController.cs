using Application.Catalog.Commands.CreateBrand;
using Application.Catalog.Commands.CreateCategory;
using Application.Catalog.Commands.CreateTaxRate;
using Application.Catalog.Commands.DeleteBrand;
using Application.Catalog.Commands.DeleteCategory;
using Application.Catalog.Commands.DeleteTaxRate;
using Application.Catalog.Commands.UpdateBrand;
using Application.Catalog.Commands.UpdateCategory;
using Application.Catalog.Commands.UpdateTaxRate;
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

    [HttpPut("brands/{brandId:int}")]
    [Authorize("Admin")]
    public async Task<IActionResult> UpdateBrand(
        int brandId,
        UpdateBrandRequest request,
        [FromServices] ICommandHandler<UpdateBrandCommand, UpdateBrandResult> handler,
        [FromServices] IValidator<UpdateBrandCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBrandCommand(brandId, request.Name);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            UpdateBrandOutcome.Updated => Ok(result),
            UpdateBrandOutcome.NotFound => NotFound(),
            _ => Problem()
        };
    }

    [HttpDelete("brands/{brandId:int}")]
    [Authorize("Admin")]
    public async Task<IActionResult> DeleteBrand(
        int brandId,
        [FromServices] ICommandHandler<DeleteBrandCommand, DeleteBrandResult> handler,
        [FromServices] IValidator<DeleteBrandCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new DeleteBrandCommand(brandId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            DeleteBrandOutcome.Deleted => Ok(result),
            DeleteBrandOutcome.NotFound => NotFound(),
            DeleteBrandOutcome.InUse => Conflict("This brand is still referenced by one or more products."),
            _ => Problem()
        };
    }

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

    [HttpPut("categories/{categoryId:int}")]
    [Authorize("Admin")]
    public async Task<IActionResult> UpdateCategory(
        int categoryId,
        UpdateCategoryRequest request,
        [FromServices] ICommandHandler<UpdateCategoryCommand, UpdateCategoryResult> handler,
        [FromServices] IValidator<UpdateCategoryCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCategoryCommand(categoryId, request.Name, request.ParentCategoryId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            UpdateCategoryOutcome.Updated => Ok(result),
            UpdateCategoryOutcome.NotFound => NotFound(),
            UpdateCategoryOutcome.ParentCategoryNotFound => NotFound("Parent category not found."),
            UpdateCategoryOutcome.SelfReference => Conflict("A category cannot be its own parent."),
            _ => Problem()
        };
    }

    [HttpDelete("categories/{categoryId:int}")]
    [Authorize("Admin")]
    public async Task<IActionResult> DeleteCategory(
        int categoryId,
        [FromServices] ICommandHandler<DeleteCategoryCommand, DeleteCategoryResult> handler,
        [FromServices] IValidator<DeleteCategoryCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new DeleteCategoryCommand(categoryId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            DeleteCategoryOutcome.Deleted => Ok(result),
            DeleteCategoryOutcome.NotFound => NotFound(),
            DeleteCategoryOutcome.InUse => Conflict("This category is still referenced by products, tax rates, or subcategories."),
            _ => Problem()
        };
    }

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

    [HttpPut("tax-rates/{taxRateId:int}")]
    [Authorize("Admin")]
    public async Task<IActionResult> UpdateTaxRate(
        int taxRateId,
        UpdateTaxRateRequest request,
        [FromServices] ICommandHandler<UpdateTaxRateCommand, UpdateTaxRateResult> handler,
        [FromServices] IValidator<UpdateTaxRateCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTaxRateCommand(taxRateId, request.Name, request.Percentage, request.CategoryId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            UpdateTaxRateOutcome.Updated => Ok(result),
            UpdateTaxRateOutcome.NotFound => NotFound(),
            UpdateTaxRateOutcome.CategoryNotFound => NotFound("Category not found."),
            _ => Problem()
        };
    }

    [HttpDelete("tax-rates/{taxRateId:int}")]
    [Authorize("Admin")]
    public async Task<IActionResult> DeleteTaxRate(
        int taxRateId,
        [FromServices] ICommandHandler<DeleteTaxRateCommand, DeleteTaxRateResult> handler,
        [FromServices] IValidator<DeleteTaxRateCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new DeleteTaxRateCommand(taxRateId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            DeleteTaxRateOutcome.Deleted => Ok(result),
            DeleteTaxRateOutcome.NotFound => NotFound(),
            DeleteTaxRateOutcome.InUse => Conflict("This tax rate is still referenced by one or more products."),
            _ => Problem()
        };
    }
}
