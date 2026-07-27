namespace Application.Catalog.Commands.DeleteCategory;

public enum DeleteCategoryOutcome
{
    Deleted,
    NotFound,
    InUse
}

public sealed record DeleteCategoryResult(DeleteCategoryOutcome Outcome);
