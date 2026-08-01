using Application.Abstractions;
using Application.Common;

namespace Application.Catalog.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
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

        category.Name = command.Name;
        category.ParentCategoryId = command.ParentCategoryId;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateCategoryResult(UpdateCategoryOutcome.Updated);
    }
}
