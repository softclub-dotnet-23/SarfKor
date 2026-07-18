namespace Application.Catalog.Commands.CreateProductBundle;

public enum CreateProductBundleOutcome
{
    Created,
    StoreNotFound,
    Forbidden
}

public sealed record CreateProductBundleResult(CreateProductBundleOutcome Outcome, int? ProductBundleId);
