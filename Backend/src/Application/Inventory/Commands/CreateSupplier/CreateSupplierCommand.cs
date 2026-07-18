namespace Application.Inventory.Commands.CreateSupplier;

public sealed record CreateSupplierCommand(string Name, string? ContactPhone, string? ContactEmail);
