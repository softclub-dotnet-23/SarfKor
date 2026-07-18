namespace Application.Catalog.Commands.CreateProductBundle;

public sealed record CreateProductBundleItemInput(int ProductId, int Quantity);

public sealed record CreateProductBundleCommand(
    int StoreId,
    string Name,
    decimal BundlePrice,
    string Currency,
    IReadOnlyList<CreateProductBundleItemInput> Items,
    string PerformedByUserId);
