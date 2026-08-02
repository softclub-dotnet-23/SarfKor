namespace Application.Inventory.Queries.GetSuppliers;

public sealed record SupplierDto(int SupplierId, string Name, string? ContactPhone, string? ContactEmail);

public enum GetSuppliersOutcome
{
    Found,
    StoreNotFound,
    Forbidden
}

public sealed record GetSuppliersResult(GetSuppliersOutcome Outcome, IReadOnlyList<SupplierDto>? Suppliers);
