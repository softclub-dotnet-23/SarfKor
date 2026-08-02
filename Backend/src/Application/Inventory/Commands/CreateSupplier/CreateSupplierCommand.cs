namespace Application.Inventory.Commands.CreateSupplier;

public sealed record CreateSupplierCommand(int StoreId, string PerformedByUserId, string Name, string? ContactPhone, string? ContactEmail);
