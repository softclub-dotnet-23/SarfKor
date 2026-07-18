using Application.Abstractions;
using Application.Common;
using Domain.Catalog;

namespace Application.Catalog.Commands.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateCategoryCommand, CreateCategoryResult>
{
    public async Task<CreateCategoryResult> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        if (command.ParentCategoryId.HasValue && !await categoryRepository.ExistsAsync(command.ParentCategoryId.Value, cancellationToken))
            return new CreateCategoryResult(CreateCategoryOutcome.ParentCategoryNotFound, null);

        var category = new Category { Name = command.Name, ParentCategoryId = command.ParentCategoryId };
        categoryRepository.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateCategoryResult(CreateCategoryOutcome.Created, category.Id);
    }
}
