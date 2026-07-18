namespace Application.Products.Queries.CompareStoresForShoppingList;

public sealed record CompareStoresForShoppingListQuery(IReadOnlyList<int> ProductIds, double? UserLatitude, double? UserLongitude);
