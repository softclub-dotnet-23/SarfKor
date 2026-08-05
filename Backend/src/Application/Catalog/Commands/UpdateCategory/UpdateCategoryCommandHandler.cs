using System.Text.Json;
using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Catalog;

namespace Application.Catalog.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(ICategoryRepository categoryRepository, IAuditLogRepository auditLogRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateCategoryCommand, UpdateCategoryResult>
{
    public async Task<UpdateCategoryResult> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
            return new UpdateCategoryResult(UpdateCategoryOutcome.NotFound);

        if (command.ParentCategoryId == command.CategoryId)
            return new UpdateCategoryResult(UpdateCategoryOutcome.SelfReference);

        if (command.ParentCategoryId.HasValue)
        {
            if (!await categoryRepository.ExistsAsync(command.ParentCategoryId.Value, cancellationToken))
                return new UpdateCategoryResult(UpdateCategoryOutcome.ParentCategoryNotFound);

            // Walks the new parent's ancestor chain (not just the direct parent) — otherwise a
            // deeper cycle (A -> B -> C -> A) silently corrupts the tree into an infinite loop for
            // anything that walks it (breadcrumbs, category listings). Bounded so a pre-existing
            // cycle elsewhere in the data can't turn this into an infinite loop itself.
            var ancestorId = command.ParentCategoryId;
            for (var depth = 0; ancestorId.HasValue && depth < 50; depth++)
            {
                if (ancestorId.Value == command.CategoryId)
                    return new UpdateCategoryResult(UpdateCategoryOutcome.SelfReference);

                var ancestor = await categoryRepository.GetByIdAsync(ancestorId.Value, cancellationToken);
                ancestorId = ancestor?.ParentCategoryId;
            }
        }

        var before = JsonSerializer.Serialize(new { name = category.Name, parentCategoryId = category.ParentCategoryId, displayOrder = category.DisplayOrder, isHidden = category.IsHidden });

        category.Name = command.Name;
        category.ParentCategoryId = command.ParentCategoryId;
        category.DisplayOrder = command.DisplayOrder;
        category.IsHidden = command.IsHidden;

        var after = JsonSerializer.Serialize(new { name = category.Name, parentCategoryId = category.ParentCategoryId, displayOrder = category.DisplayOrder, isHidden = category.IsHidden });

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "Category.Updated",
            EntityType = nameof(Category),
            EntityId = category.Id,
            IpAddress = command.PerformedByIpAddress,
            BeforeStateJson = before,
            AfterStateJson = after,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateCategoryResult(UpdateCategoryOutcome.Updated);
    }
}
