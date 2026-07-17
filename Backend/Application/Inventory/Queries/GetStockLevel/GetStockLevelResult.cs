namespace Application.Inventory.Queries.GetStockLevel;

public enum GetStockLevelOutcome
{
    Found,
    StoreNotFound,
    Forbidden
}

public sealed record StockLevelDto(int ProductId, int Quantity);

public sealed record GetStockLevelResult(GetStockLevelOutcome Outcome, IReadOnlyList<StockLevelDto>? Levels);
