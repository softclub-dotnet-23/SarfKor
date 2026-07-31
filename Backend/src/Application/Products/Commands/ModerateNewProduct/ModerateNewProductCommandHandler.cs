using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Products;

namespace Application.Products.Commands.ModerateNewProduct;

public sealed class ModerateNewProductCommandHandler(
    IProductSubmissionRepository productSubmissionRepository,
    IProductRepository productRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ModerateNewProductCommand, ModerateNewProductResult>
{
    public async Task<ModerateNewProductResult> Handle(ModerateNewProductCommand command, CancellationToken cancellationToken)
    {
        var submission = await productSubmissionRepository.GetByIdAsync(command.ProductSubmissionId, cancellationToken);
        if (submission is null)
            return new ModerateNewProductResult(ModerateNewProductOutcome.NotFound, null);

        if (submission.Status != ProductSubmissionStatus.Pending)
            return new ModerateNewProductResult(ModerateNewProductOutcome.AlreadyModerated, null);

        // A StorePartner self-approving may only ever act on their own submission — Admin (the only
        // other caller) passes RequireOwnSubmission=false and can moderate anyone's.
        if (command.RequireOwnSubmission && submission.SubmittedByUserId != command.PerformedByUserId)
            return new ModerateNewProductResult(ModerateNewProductOutcome.Forbidden, null);

        Product? product = null;

        if (command.Approve)
        {
            // Re-checked here, not just at submit time: a Product with this barcode could have been
            // created by a different, since-approved submission (or another path entirely) in the
            // time between this submission being filed and moderated.
            var existingProduct = await productRepository.GetByBarcodeAsync(submission.Barcode.Value, cancellationToken);
            if (existingProduct is not null)
            {
                submission.Status = ProductSubmissionStatus.Rejected;
                submission.ModeratedByAdminUserId = command.PerformedByUserId;
                submission.ModeratedAt = DateTimeOffset.UtcNow;

                auditLogRepository.Add(new AuditLog
                {
                    PerformedByUserId = command.PerformedByUserId,
                    Action = "ProductSubmission.RejectedDuplicate",
                    EntityType = nameof(ProductSubmission),
                    EntityId = submission.Id,
                    Details = $"Barcode {submission.Barcode.Value} already exists as Product {existingProduct.Id}",
                    OccurredAt = DateTimeOffset.UtcNow
                });

                await unitOfWork.SaveChangesAsync(cancellationToken);
                return new ModerateNewProductResult(ModerateNewProductOutcome.DuplicateBarcode, existingProduct.Id);
            }

            product = new Product
            {
                Barcode = submission.Barcode,
                Name = submission.Name,
                CategoryId = submission.CategoryId,
                BrandId = submission.BrandId,
                CountryOfOrigin = submission.CountryOfOrigin
            };

            productRepository.Add(product);
            submission.Status = ProductSubmissionStatus.Approved;
        }
        else
        {
            submission.Status = ProductSubmissionStatus.Rejected;
        }

        submission.ModeratedByAdminUserId = command.PerformedByUserId;
        submission.ModeratedAt = DateTimeOffset.UtcNow;

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = command.Approve ? "ProductSubmission.Approved" : "ProductSubmission.Rejected",
            EntityType = nameof(ProductSubmission),
            EntityId = submission.Id,
            Details = command.Reason,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ModerateNewProductResult(
            command.Approve ? ModerateNewProductOutcome.Approved : ModerateNewProductOutcome.Rejected,
            product?.Id);
    }
}
