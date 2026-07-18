namespace Application.Inventory.Queries.GetSuppliers;

public sealed record SupplierDto(int SupplierId, string Name, string? ContactPhone, string? ContactEmail);

public sealed record GetSuppliersResult(IReadOnlyList<SupplierDto> Suppliers);
