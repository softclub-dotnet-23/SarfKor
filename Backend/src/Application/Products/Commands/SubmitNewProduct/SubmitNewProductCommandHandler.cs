using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Products;
using Domain.ValueObjects;

namespace Application.Products.Commands.SubmitNewProduct;

// ADMIN_PROMPT.md §1: no moderation queue at all anymore — every submission publishes a Product
// immediately, whether it came from a trusted StorePartner (CreateDirectly=true, the original fast
// path) or an ordinary user (CreateDirectly=false). ProductSubmission survives purely as a
// provenance record ("who introduced this to the catalog"), always linked to the Product it
// created via ProductId, never a separate pending state anyone waits on.
public sealed class SubmitNewProductCommandHandler(
    IProductRepository productRepository,
    IProductSubmissionRepository productSubmissionRepository,
    ICategoryRepository categoryRepository,
    IBrandRepository brandRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<SubmitNewProductCommand, SubmitNewProductResult>
{
    public async Task<SubmitNewProductResult> Handle(SubmitNewProductCommand command, CancellationToken cancellationToken)
    {
        var existingProduct = await productRepository.GetByBarcodeAsync(command.Barcode, cancellationToken);
        if (existingProduct is not null)
            return new SubmitNewProductResult(SubmitNewProductOutcome.DuplicateBarcode, null);

        if (!await categoryRepository.ExistsAsync(command.CategoryId, cancellationToken))
            return new SubmitNewProductResult(SubmitNewProductOutcome.CategoryNotFound, null);

        if (!await brandRepository.ExistsAsync(command.BrandId, cancellationToken))
            return new SubmitNewProductResult(SubmitNewProductOutcome.BrandNotFound, null);

        var product = new Product
        {
            Barcode = new Barcode(command.Barcode),
            Name = command.Name,
            CategoryId = command.CategoryId,
            BrandId = command.BrandId,
            CountryOfOrigin = command.CountryOfOrigin
        };
        productRepository.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var submission = new ProductSubmission
        {
            Barcode = new Barcode(command.Barcode),
            Name = command.Name,
            CategoryId = command.CategoryId,
            BrandId = command.BrandId,
            CountryOfOrigin = command.CountryOfOrigin,
            SubmittedByUserId = command.SubmittedByUserId,
            ProductId = product.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        productSubmissionRepository.Add(submission);

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.SubmittedByUserId,
            Action = command.CreateDirectly ? "Product.CreatedByPartner" : "Product.CreatedByUser",
            EntityType = nameof(Product),
            EntityId = product.Id,
            Details = command.Barcode,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SubmitNewProductResult(SubmitNewProductOutcome.Created, submission.Id, product.Id);
    }
}
