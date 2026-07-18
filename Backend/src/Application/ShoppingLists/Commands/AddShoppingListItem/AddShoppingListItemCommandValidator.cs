using FluentValidation;

namespace Application.ShoppingLists.Commands.AddShoppingListItem;

public sealed class AddShoppingListItemCommandValidator : AbstractValidator<AddShoppingListItemCommand>
{
    public AddShoppingListItemCommandValidator()
    {
        RuleFor(x => x.ShoppingListId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
