using FluentValidation;

namespace Application.ShoppingLists.Commands.RemoveShoppingListItem;

public sealed class RemoveShoppingListItemCommandValidator : AbstractValidator<RemoveShoppingListItemCommand>
{
    public RemoveShoppingListItemCommandValidator()
    {
        RuleFor(x => x.ShoppingListId).GreaterThan(0);
        RuleFor(x => x.ItemId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
    }
}
