namespace Application.Inventory.Commands.CreateReorderRule;

public sealed record CreateReorderRuleCommand(
    int StoreId,
    int ProductId,
    int ThresholdQuantity,
    int ReorderQuantity,
    int? PreferredSupplierId,
    string PerformedByUserId);
