namespace Application.Catalog.Commands.UpdateCategory;

public enum UpdateCategoryOutcome
{
    Updated,
    NotFound,
    ParentCategoryNotFound,
    SelfReference
}

public sealed record UpdateCategoryResult(UpdateCategoryOutcome Outcome);
