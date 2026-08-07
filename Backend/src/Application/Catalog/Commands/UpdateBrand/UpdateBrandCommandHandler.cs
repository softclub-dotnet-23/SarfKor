using System.Text.Json;
using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Catalog;

namespace Application.Catalog.Commands.UpdateBrand;

public sealed class UpdateBrandCommandHandler(IBrandRepository brandRepository, IAuditLogRepository auditLogRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateBrandCommand, UpdateBrandResult>
{
    public async Task<UpdateBrandResult> Handle(UpdateBrandCommand command, CancellationToken cancellationToken)
    {
        var brand = await brandRepository.GetByIdAsync(command.BrandId, cancellationToken);
        if (brand is null)
            return new UpdateBrandResult(UpdateBrandOutcome.NotFound);

        var previousName = brand.Name;
        brand.Name = command.Name;

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "Brand.Updated",
            EntityType = nameof(Brand),
            EntityId = brand.Id,
            IpAddress = command.PerformedByIpAddress,
            BeforeStateJson = JsonSerializer.Serialize(new { name = previousName }),
            AfterStateJson = JsonSerializer.Serialize(new { name = command.Name }),
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateBrandResult(UpdateBrandOutcome.Updated);
    }
}
