using FluentValidation;

namespace Application.Inventory.Queries.GetStockTransfers;

public sealed class GetStockTransfersQueryValidator : AbstractValidator<GetStockTransfersQuery>
{
    public GetStockTransfersQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}
