using System.Security.Claims;
using Application.Common;
using Application.Products.Commands.ModerateNewProduct;
using Application.Products.Commands.RecordScan;
using Application.Products.Commands.SubmitNewProduct;
using Application.Products.Queries.CompareStoresForShoppingList;
using Application.Products.Queries.GetMostScannedProducts;
using Application.Products.Queries.GetProductById;
using Application.Products.Queries.GetTopSellingProducts;
using Application.Products.Queries.ScanBarcode;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    [HttpGet("{productId:int}")]
    public async Task<IActionResult> GetProduct(
        int productId,
        [FromServices] IQueryHandler<GetProductByIdQuery, GetProductByIdResult?> handler,
        [FromServices] IValidator<GetProductByIdQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(productId);

        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("scan/{barcode}")]
    [EnableRateLimiting("scan")]
    public async Task<IActionResult> ScanBarcode(
        string barcode,
        double? lat,
        double? lng,
        [FromServices] IQueryHandler<ScanBarcodeQuery, ScanBarcodeResult?> handler,
        [FromServices] IValidator<ScanBarcodeQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new ScanBarcodeQuery(barcode, lat, lng);

        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("/api/scans")]
    [EnableRateLimiting("scan")]
    public async Task<IActionResult> RecordScan(
        RecordScanRequest request,
        [FromServices] ICommandHandler<RecordScanCommand, RecordScanResult> handler,
        [FromServices] IValidator<RecordScanCommand> validator,
        CancellationToken cancellationToken)
    {
        // Recording a scan is deliberately separate from ScanBarcodeQuery — a query stays read-only,
        // and an anonymous shopper can still scan without an account (UserId stays null for them).
        var command = new RecordScanCommand(request.ProductId, User.FindFirstValue(ClaimTypes.NameIdentifier), request.StoreId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            RecordScanOutcome.Recorded => Ok(result),
            RecordScanOutcome.ProductNotFound => NotFound(),
            RecordScanOutcome.StoreNotFound => NotFound(),
            _ => Problem()
        };
    }

    [HttpGet("most-scanned")]
    [EnableRateLimiting("scan")]
    public async Task<IActionResult> GetMostScanned(
        int? limit,
        [FromServices] IQueryHandler<GetMostScannedProductsQuery, GetMostScannedProductsResult> handler,
        [FromServices] IValidator<GetMostScannedProductsQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetMostScannedProductsQuery(limit ?? 10);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        return Ok(await handler.Handle(query, cancellationToken));
    }

    [HttpGet("top-selling")]
    [EnableRateLimiting("scan")]
    public async Task<IActionResult> GetTopSelling(
        int? storeId,
        int? limit,
        [FromServices] IQueryHandler<GetTopSellingProductsQuery, GetTopSellingProductsResult> handler,
        [FromServices] IValidator<GetTopSellingProductsQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new GetTopSellingProductsQuery(storeId, limit ?? 10);

        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("compare-basket")]
    [EnableRateLimiting("scan")]
    public async Task<IActionResult> CompareBasket(
        [FromQuery] int[] productIds,
        double? lat,
        double? lng,
        [FromServices] IQueryHandler<CompareStoresForShoppingListQuery, CompareStoresForShoppingListResult> handler,
        [FromServices] IValidator<CompareStoresForShoppingListQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new CompareStoresForShoppingListQuery(productIds, lat, lng);

        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(query, cancellationToken);
        return Ok(result);
    }

    // Scanning an unrecognized barcode is the only way anyone finds out a product isn't in the
    // catalog yet — this is the missing other half of that flow (ModerateNewProductCommand could
    // approve/reject a submission, but nothing ever created one).
    [HttpPost("submissions")]
    [Authorize]
    [EnableRateLimiting("contributions")]
    public async Task<IActionResult> SubmitNewProduct(
        SubmitNewProductRequest request,
        [FromServices] ICommandHandler<SubmitNewProductCommand, SubmitNewProductResult> handler,
        [FromServices] IValidator<SubmitNewProductCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new SubmitNewProductCommand(
            request.Barcode, request.Name, request.CategoryId, request.BrandId, request.CountryOfOrigin, userId,
            CreateDirectly: User.IsInRole("StorePartner"));

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            SubmitNewProductOutcome.Submitted => Ok(result),
            SubmitNewProductOutcome.Created => Ok(result),
            SubmitNewProductOutcome.DuplicateBarcode => Conflict("A product with this barcode already exists."),
            SubmitNewProductOutcome.DuplicatePendingSubmission => Conflict("A submission for this barcode is already pending moderation."),
            SubmitNewProductOutcome.CategoryNotFound => NotFound("Category not found."),
            SubmitNewProductOutcome.BrandNotFound => NotFound("Brand not found."),
            _ => Problem()
        };
    }

    // A StorePartner adding a product nobody's catalogued yet no longer needs to wait on an Admin —
    // restricted to their own submission only (RequireOwnSubmission=true in the handler), so this
    // can't be used to approve/reject someone else's pending item. Admin's own moderation endpoint
    // (AdminController.ModerateNewProduct) is untouched and can still act on anyone's.
    [HttpPost("submissions/{submissionId:int}/self-approve")]
    [Authorize(Roles = "StorePartner")]
    [EnableRateLimiting("contributions")]
    public async Task<IActionResult> SelfApproveNewProduct(
        int submissionId,
        [FromServices] ICommandHandler<ModerateNewProductCommand, ModerateNewProductResult> handler,
        [FromServices] IValidator<ModerateNewProductCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var command = new ModerateNewProductCommand(submissionId, Approve: true, userId, Reason: null, RequireOwnSubmission: true);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return this.ToValidationProblem(validationResult);

        var result = await handler.Handle(command, cancellationToken);
        return result.Outcome switch
        {
            ModerateNewProductOutcome.Approved => Ok(result),
            ModerateNewProductOutcome.NotFound => NotFound(),
            ModerateNewProductOutcome.Forbidden => Forbid(),
            ModerateNewProductOutcome.AlreadyModerated => Conflict("This submission has already been moderated."),
            ModerateNewProductOutcome.DuplicateBarcode => Conflict("A product with this barcode already exists — submission auto-rejected."),
            _ => Problem()
        };
    }
}
