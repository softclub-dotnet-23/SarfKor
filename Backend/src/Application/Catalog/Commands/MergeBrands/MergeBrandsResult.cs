namespace Application.Catalog.Commands.MergeBrands;

public enum MergeBrandsOutcome
{
    Merged,
    TargetNotFound,
    SourceNotFound,
    TargetInSourceList
}

public sealed record MergeBrandsResult(MergeBrandsOutcome Outcome, int ProductsMoved);
