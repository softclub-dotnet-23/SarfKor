namespace Application.ShoppingLists.Commands.AddShoppingListItem;

public enum AddShoppingListItemOutcome
{
    Added,
    ListNotFound,
    Forbidden
}

public sealed record AddShoppingListItemResult(AddShoppingListItemOutcome Outcome, int? ItemId);
