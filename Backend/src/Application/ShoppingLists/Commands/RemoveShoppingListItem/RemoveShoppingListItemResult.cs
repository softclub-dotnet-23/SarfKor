namespace Application.ShoppingLists.Commands.RemoveShoppingListItem;

public enum RemoveShoppingListItemOutcome
{
    Removed,
    ListNotFound,
    ItemNotFound,
    Forbidden
}

public sealed record RemoveShoppingListItemResult(RemoveShoppingListItemOutcome Outcome);
