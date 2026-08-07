using Application.Catalog.Commands.CreateBrand;
using Application.Catalog.Commands.CreateCategory;
using Application.Catalog.Commands.CreateTaxRate;
using Application.Catalog.Commands.DeleteBrand;
using Application.Catalog.Commands.DeleteCategory;
using Application.Catalog.Commands.DeleteTaxRate;
using Application.Catalog.Commands.MergeBrands;
using Application.Catalog.Commands.UpdateBrand;
using Application.Catalog.Commands.UpdateCategory;
using Application.Catalog.Commands.UpdateTaxRate;
using Application.Catalog.Queries.GetBrandDuplicateCandidates;
using Application.Catalog.Queries.GetBrands;
using Application.Catalog.Queries.GetCategories;
using Application.Catalog.Queries.GetTaxRates;
using Application.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace WebApi.Controllers;

// Catalog reference data (Brand/Category/TaxRate) — shared, platform-wide lookup tables that
// Product/StockMovement already reference by FK. All lists are public — the frontend needs them
// for dropdowns/browsing. Any StorePartner can add a new Brand/Category outright (no moderation
// queue at all anymore, ADMIN_PROMPT.md §1 — new Products publish immediately too), but
// editing/deleting an existing one stays Admin-only since that can affect every other store
// already referencing it, not just the caller's own.
[ApiController]
[Route("api/catalog")]
public sealed class CatalogController : ControllerBase
{
    [HttpPost("brands")]
    [Authorize(Roles = "Admin,StorePartner")]
    [EnableRateLimiting("partner-write")]
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
        return result.Outcome switch
        {
            CreateBrandOutcome.Created => Ok(result),
            CreateBrandOutcome.AlreadyExists => Conflict("A brand with this name already exists."),
            _ => Problem()
        };
    }

    [HttpGet("brands")]
    public async Task<IActionResult> GetBrands(
        string? search,
        [FromServices] IQueryHandler<GetBrandsQuery, GetBrandsResult> handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.Handle(new GetBrandsQuery(search), cancellationToken));

    [HttpPut("brands/{brandId:int}")]
    [Authorize("Admin")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> UpdateBrand(
        int brandId,
        UpdateBrandRequest request,
        [FromServices] ICommandHandler<UpdateBrandCommand, UpdateBrandResult> handler,
        [FromServices] IValidator<UpdateBrandCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new UpdateBrandCommand(brandId, request.Name, userId, HttpContext.Connection.RemoteIpAddress?.ToString());

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
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> DeleteBrand(
        int brandId,
        [FromServices] ICommandHandler<DeleteBrandCommand, DeleteBrandResult> handler,
        [FromServices] IValidator<DeleteBrandCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new DeleteBrandCommand(brandId, userId, HttpContext.Connection.RemoteIpAddress?.ToString());

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
    [Authorize(Roles = "Admin,StorePartner")]
    [EnableRateLimiting("partner-write")]
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
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> UpdateCategory(
        int categoryId,
        UpdateCategoryRequest request,
        [FromServices] ICommandHandler<UpdateCategoryCommand, UpdateCategoryResult> handler,
        [FromServices] IValidator<UpdateCategoryCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new UpdateCategoryCommand(
            categoryId, request.Name, request.ParentCategoryId, request.DisplayOrder, request.IsHidden,
            userId, HttpContext.Connection.RemoteIpAddress?.ToString());

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
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> DeleteCategory(
        int categoryId,
        [FromServices] ICommandHandler<DeleteCategoryCommand, DeleteCategoryResult> handler,
        [FromServices] IValidator<DeleteCategoryCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new DeleteCategoryCommand(categoryId, userId, HttpContext.Connection.RemoteIpAddress?.ToString());

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
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> CreateTaxRate(
        CreateTaxRateRequest request,
        [FromServices] ICommandHandler<CreateTaxRateCommand, CreateTaxRateResult> handler,
        [FromServices] IValidator<CreateTaxRateCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new CreateTaxRateCommand(
            request.Name, request.Percentage, request.CategoryId, request.EffectiveFrom, request.EffectiveTo,
            userId, HttpContext.Connection.RemoteIpAddress?.ToString());

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            CreateTaxRateOutcome.Created => Ok(result),
            CreateTaxRateOutcome.CategoryNotFound => NotFound("Category not found."),
            _ => Problem()
        };
    }

    [HttpGet("tax-rates")]
    public async Task<IActionResult> GetTaxRates(
        [FromServices] IQueryHandler<GetTaxRatesQuery, GetTaxRatesResult> handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.Handle(new GetTaxRatesQuery(), cancellationToken));

    [HttpPut("tax-rates/{taxRateId:int}")]
    [Authorize("Admin")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> UpdateTaxRate(
        int taxRateId,
        UpdateTaxRateRequest request,
        [FromServices] ICommandHandler<UpdateTaxRateCommand, UpdateTaxRateResult> handler,
        [FromServices] IValidator<UpdateTaxRateCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new UpdateTaxRateCommand(
            taxRateId, request.Name, request.Percentage, request.CategoryId, request.EffectiveFrom, request.EffectiveTo,
            userId, HttpContext.Connection.RemoteIpAddress?.ToString());

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
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> DeleteTaxRate(
        int taxRateId,
        [FromServices] ICommandHandler<DeleteTaxRateCommand, DeleteTaxRateResult> handler,
        [FromServices] IValidator<DeleteTaxRateCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new DeleteTaxRateCommand(taxRateId, userId, HttpContext.Connection.RemoteIpAddress?.ToString());

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

    // ADMIN_PROMPT.md §2.8: brand cleanup, not brand creation — see GetBrands' Search param and
    // this pair for the "чистка дубликатов" workflow the class-level comment above already
    // describes as the intended Admin brand management surface.
    [HttpGet("brands/duplicate-candidates")]
    [Authorize("Admin")]
    public async Task<IActionResult> GetBrandDuplicateCandidates(
        [FromServices] IQueryHandler<GetBrandDuplicateCandidatesQuery, GetBrandDuplicateCandidatesResult> handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.Handle(new GetBrandDuplicateCandidatesQuery(), cancellationToken));

    [HttpPost("brands/merge")]
    [Authorize("Admin")]
    [EnableRateLimiting("partner-write")]
    public async Task<IActionResult> MergeBrands(
        MergeBrandsRequest request,
        [FromServices] ICommandHandler<MergeBrandsCommand, MergeBrandsResult> handler,
        [FromServices] IValidator<MergeBrandsCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new MergeBrandsCommand(request.TargetBrandId, request.SourceBrandIds, userId, HttpContext.Connection.RemoteIpAddress?.ToString());

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            MergeBrandsOutcome.Merged => Ok(result),
            MergeBrandsOutcome.TargetNotFound => NotFound("Target brand not found."),
            MergeBrandsOutcome.SourceNotFound => NotFound("One or more source brands not found."),
            MergeBrandsOutcome.TargetInSourceList => Conflict("Target brand cannot also be one of the source brands."),
            _ => Problem()
        };
    }
}
