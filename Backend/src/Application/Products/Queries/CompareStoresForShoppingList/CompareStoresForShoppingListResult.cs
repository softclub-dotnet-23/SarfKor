namespace Application.Products.Queries.CompareStoresForShoppingList;

public sealed record StoreBasketDto(int StoreId, string StoreName, decimal TotalPrice, string Currency, double? DistanceKm);

public sealed record CompareStoresForShoppingListResult(IReadOnlyList<StoreBasketDto> Stores);
