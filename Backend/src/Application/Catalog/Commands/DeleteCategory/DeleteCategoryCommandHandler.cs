using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Catalog;

namespace Application.Catalog.Commands.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(ICategoryRepository categoryRepository, IAuditLogRepository auditLogRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteCategoryCommand, DeleteCategoryResult>
{
    public async Task<DeleteCategoryResult> Handle(DeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
            return new DeleteCategoryResult(DeleteCategoryOutcome.NotFound);

        if (await categoryRepository.IsInUseAsync(command.CategoryId, cancellationToken))
            return new DeleteCategoryResult(DeleteCategoryOutcome.InUse);

        categoryRepository.Remove(category);

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "Category.Deleted",
            EntityType = nameof(Category),
            EntityId = category.Id,
            Details = category.Name,
            IpAddress = command.PerformedByIpAddress,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteCategoryResult(DeleteCategoryOutcome.Deleted);
    }
}
