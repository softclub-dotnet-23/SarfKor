namespace Application.ShoppingLists.Commands.AddShoppingListItem;

public sealed record AddShoppingListItemCommand(int ShoppingListId, string UserId, int ProductId, int Quantity);
