namespace Application.Stores.Commands.ApproveStore;

public sealed record ApproveStoreCommand(int StoreId, string PerformedByUserId, string? PerformedByIpAddress = null);
