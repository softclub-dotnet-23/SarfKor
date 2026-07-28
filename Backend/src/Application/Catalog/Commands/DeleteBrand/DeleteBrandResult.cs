namespace Application.Catalog.Commands.DeleteBrand;

public enum DeleteBrandOutcome
{
    Deleted,
    NotFound,
    InUse
}

public sealed record DeleteBrandResult(DeleteBrandOutcome Outcome);
