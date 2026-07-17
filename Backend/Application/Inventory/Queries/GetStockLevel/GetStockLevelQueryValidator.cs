using FluentValidation;

namespace Application.Inventory.Queries.GetStockLevel;

public sealed class GetStockLevelQueryValidator : AbstractValidator<GetStockLevelQuery>
{
    public GetStockLevelQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}
