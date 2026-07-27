namespace Application.Catalog.Commands.UpdateBrand;

public enum UpdateBrandOutcome
{
    Updated,
    NotFound
}

public sealed record UpdateBrandResult(UpdateBrandOutcome Outcome);
