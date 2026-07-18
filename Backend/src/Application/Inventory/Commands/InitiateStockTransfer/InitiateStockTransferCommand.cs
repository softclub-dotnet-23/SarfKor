namespace Application.Inventory.Commands.InitiateStockTransfer;

public sealed record InitiateStockTransferCommand(int ProductId, int FromStoreId, int ToStoreId, int Quantity, string PerformedByUserId);
