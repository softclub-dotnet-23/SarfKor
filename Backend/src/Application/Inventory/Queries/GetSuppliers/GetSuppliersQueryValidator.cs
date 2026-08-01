using FluentValidation;

namespace Application.Inventory.Queries.GetSuppliers;

public sealed class GetSuppliersQueryValidator : AbstractValidator<GetSuppliersQuery>
{
    public GetSuppliersQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}
