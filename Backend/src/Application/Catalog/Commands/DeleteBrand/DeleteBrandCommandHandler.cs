using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Catalog;

namespace Application.Catalog.Commands.DeleteBrand;

public sealed class DeleteBrandCommandHandler(IBrandRepository brandRepository, IAuditLogRepository auditLogRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteBrandCommand, DeleteBrandResult>
{
    public async Task<DeleteBrandResult> Handle(DeleteBrandCommand command, CancellationToken cancellationToken)
    {
        var brand = await brandRepository.GetByIdAsync(command.BrandId, cancellationToken);
        if (brand is null)
            return new DeleteBrandResult(DeleteBrandOutcome.NotFound);

        if (await brandRepository.IsInUseAsync(command.BrandId, cancellationToken))
            return new DeleteBrandResult(DeleteBrandOutcome.InUse);

        brandRepository.Remove(brand);

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "Brand.Deleted",
            EntityType = nameof(Brand),
            EntityId = brand.Id,
            Details = brand.Name,
            IpAddress = command.PerformedByIpAddress,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteBrandResult(DeleteBrandOutcome.Deleted);
    }
}
