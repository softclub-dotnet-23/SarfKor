using FluentValidation;

namespace Application.Sales.Queries.GetReturnsForSale;

public sealed class GetReturnsForSaleQueryValidator : AbstractValidator<GetReturnsForSaleQuery>
{
    public GetReturnsForSaleQueryValidator()
    {
        RuleFor(x => x.SaleTransactionId).GreaterThan(0);
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}
