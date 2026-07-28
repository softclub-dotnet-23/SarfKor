using Application.Abstractions;
using Application.Common;
using Domain.Products;
using Domain.ValueObjects;

namespace Application.Products.Commands.SubmitNewProduct;

public sealed class SubmitNewProductCommandHandler(
    IProductRepository productRepository,
    IProductSubmissionRepository productSubmissionRepository,
    ICategoryRepository categoryRepository,
    IBrandRepository brandRepository,
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

        // Two shoppers scanning the same unrecognized barcode is the expected case, not the
        // exception — without this check every one of them would create their own duplicate
        // submission for the same product.
        var existingPending = await productSubmissionRepository.GetPendingByBarcodeAsync(command.Barcode, cancellationToken);
        if (existingPending is not null)
            return new SubmitNewProductResult(SubmitNewProductOutcome.DuplicatePendingSubmission, null);

        var submission = new ProductSubmission
        {
            Barcode = new Barcode(command.Barcode),
            Name = command.Name,
            CategoryId = command.CategoryId,
            BrandId = command.BrandId,
            CountryOfOrigin = command.CountryOfOrigin,
            SubmittedByUserId = command.SubmittedByUserId,
            Status = ProductSubmissionStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        productSubmissionRepository.Add(submission);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SubmitNewProductResult(SubmitNewProductOutcome.Submitted, submission.Id);
    }
}
