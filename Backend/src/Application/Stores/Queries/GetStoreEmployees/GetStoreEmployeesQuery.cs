namespace Application.Stores.Queries.GetStoreEmployees;

public sealed record GetStoreEmployeesQuery(int StoreId, string RequestedByUserId);
