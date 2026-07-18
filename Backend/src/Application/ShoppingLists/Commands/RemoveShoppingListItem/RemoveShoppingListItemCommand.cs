namespace Application.ShoppingLists.Commands.RemoveShoppingListItem;

public sealed record RemoveShoppingListItemCommand(int ShoppingListId, int ItemId, string UserId);
