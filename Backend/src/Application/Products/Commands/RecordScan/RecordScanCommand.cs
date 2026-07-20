namespace Application.Products.Commands.RecordScan;

public sealed record RecordScanCommand(int ProductId, string? UserId, int? StoreId);
