namespace Application.Catalog.Commands.CreateCategory;

public enum CreateCategoryOutcome
{
    Created,
    ParentCategoryNotFound
}

public sealed record CreateCategoryResult(CreateCategoryOutcome Outcome, int? CategoryId);
