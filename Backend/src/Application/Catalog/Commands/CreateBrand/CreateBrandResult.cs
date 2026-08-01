namespace Application.Catalog.Commands.CreateBrand;

public enum CreateBrandOutcome
{
    Created,
    AlreadyExists
}

public sealed record CreateBrandResult(CreateBrandOutcome Outcome, int? BrandId);
