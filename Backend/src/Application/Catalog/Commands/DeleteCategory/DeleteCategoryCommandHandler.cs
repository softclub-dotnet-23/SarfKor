using Application.Abstractions;
using Application.Common;

namespace Application.Catalog.Commands.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
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
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteCategoryResult(DeleteCategoryOutcome.Deleted);
    }
}
