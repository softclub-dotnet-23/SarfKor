namespace Application.Inventory.Commands.CreateReorderRule;

public enum CreateReorderRuleOutcome
{
    Created,
    StoreNotFound,
    Forbidden,
    ProductNotFound,
    SupplierNotFound
}

public sealed record CreateReorderRuleResult(CreateReorderRuleOutcome Outcome, int? ReorderRuleId);
