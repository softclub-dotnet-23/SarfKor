namespace Application.Inventory.Commands.DeleteSupplier;

public sealed record DeleteSupplierCommand(int SupplierId, string PerformedByUserId);
