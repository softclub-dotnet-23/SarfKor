namespace Application.ShoppingLists.Queries.GetShoppingLists;

public sealed record ShoppingListItemDto(int ItemId, int ProductId, int Quantity);

public sealed record ShoppingListDto(int ShoppingListId, string Name, IReadOnlyList<ShoppingListItemDto> Items);

public sealed record GetShoppingListsResult(IReadOnlyList<ShoppingListDto> Lists);
