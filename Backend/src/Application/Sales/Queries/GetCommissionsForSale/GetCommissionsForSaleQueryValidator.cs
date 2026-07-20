using FluentValidation;

namespace Application.Sales.Queries.GetCommissionsForSale;

public sealed class GetCommissionsForSaleQueryValidator : AbstractValidator<GetCommissionsForSaleQuery>
{
    public GetCommissionsForSaleQueryValidator()
    {
        RuleFor(x => x.SaleTransactionId).GreaterThan(0);
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}
