namespace Application.Inventory.Commands.UpdateSupplier;

public sealed record UpdateSupplierCommand(int SupplierId, string Name, string? ContactPhone, string? ContactEmail);
