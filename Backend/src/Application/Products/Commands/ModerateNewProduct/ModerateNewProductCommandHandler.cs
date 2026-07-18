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

        Product? product = null;

        if (command.Approve)
        {
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

        submission.ModeratedByAdminUserId = command.AdminUserId;
        submission.ModeratedAt = DateTimeOffset.UtcNow;

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.AdminUserId,
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
