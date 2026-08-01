namespace Application.Inventory.Queries.GetSuppliers;

public sealed record GetSuppliersQuery(int StoreId, string RequestedByUserId);
