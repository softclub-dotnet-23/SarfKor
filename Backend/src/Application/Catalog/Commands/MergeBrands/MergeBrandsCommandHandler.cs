using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Catalog;

namespace Application.Catalog.Commands.MergeBrands;

public sealed class MergeBrandsCommandHandler(
    IBrandRepository brandRepository,
    IProductRepository productRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<MergeBrandsCommand, MergeBrandsResult>
{
    public async Task<MergeBrandsResult> Handle(MergeBrandsCommand command, CancellationToken cancellationToken)
    {
        if (command.SourceBrandIds.Contains(command.TargetBrandId))
            return new MergeBrandsResult(MergeBrandsOutcome.TargetInSourceList, 0);

        var target = await brandRepository.GetByIdAsync(command.TargetBrandId, cancellationToken);
        if (target is null)
            return new MergeBrandsResult(MergeBrandsOutcome.TargetNotFound, 0);

        var sourceBrands = new List<Brand>();
        foreach (var sourceId in command.SourceBrandIds.Distinct())
        {
            var source = await brandRepository.GetByIdAsync(sourceId, cancellationToken);
            if (source is null)
                return new MergeBrandsResult(MergeBrandsOutcome.SourceNotFound, 0);
            sourceBrands.Add(source);
        }

        var totalMoved = 0;

        // One DB transaction for the whole merge (ADMIN_PROMPT.md §2.8: "слияние обязано быть
        // транзакционным") — either every source's products move and every source brand is deleted,
        // or none of it happens; ExecuteUpdateAsync inside ExecuteInTransactionAsync means a failure
        // partway through (e.g. the second source) rolls back the first source's reassignment too.
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            foreach (var source in sourceBrands)
            {
                var moved = await productRepository.ReassignBrandAsync(source.Id, target.Id, ct);
                totalMoved += moved;
                brandRepository.Remove(source);
            }

            auditLogRepository.Add(new AuditLog
            {
                PerformedByUserId = command.PerformedByUserId,
                Action = "Brand.Merged",
                EntityType = nameof(Brand),
                EntityId = target.Id,
                Details = $"Merged brands [{string.Join(", ", sourceBrands.Select(b => $"{b.Id}:{b.Name}"))}] into {target.Id}:{target.Name}, {totalMoved} products moved",
                IpAddress = command.PerformedByIpAddress,
                OccurredAt = DateTimeOffset.UtcNow
            });

            await unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        return new MergeBrandsResult(MergeBrandsOutcome.Merged, totalMoved);
    }
}
