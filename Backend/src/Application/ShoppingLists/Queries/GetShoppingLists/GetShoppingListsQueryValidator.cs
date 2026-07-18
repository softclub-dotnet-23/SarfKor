using FluentValidation;

namespace Application.ShoppingLists.Queries.GetShoppingLists;

public sealed class GetShoppingListsQueryValidator : AbstractValidator<GetShoppingListsQuery>
{
    public GetShoppingListsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
