using Domain.Inventory;

namespace Application.Inventory.Queries.GetStockTransfers;

public sealed record StockTransferDto(
    int StockTransferId,
    int ProductId,
    int FromStoreId,
    int ToStoreId,
    int Quantity,
    StockTransferStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public enum GetStockTransfersOutcome
{
    Found,
    StoreNotFound,
    Forbidden
}

public sealed record GetStockTransfersResult(GetStockTransfersOutcome Outcome, IReadOnlyList<StockTransferDto>? Transfers);
