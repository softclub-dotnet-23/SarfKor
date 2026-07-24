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

        if (command.ParentCategoryId.HasValue && !await categoryRepository.ExistsAsync(command.ParentCategoryId.Value, cancellationToken))
            return new UpdateCategoryResult(UpdateCategoryOutcome.ParentCategoryNotFound);

        category.Name = command.Name;
        category.ParentCategoryId = command.ParentCategoryId;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateCategoryResult(UpdateCategoryOutcome.Updated);
    }
}
