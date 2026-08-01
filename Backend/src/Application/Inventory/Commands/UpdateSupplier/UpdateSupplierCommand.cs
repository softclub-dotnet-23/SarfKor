namespace Application.Inventory.Commands.UpdateSupplier;

public sealed record UpdateSupplierCommand(int SupplierId, string PerformedByUserId, string Name, string? ContactPhone, string? ContactEmail);
