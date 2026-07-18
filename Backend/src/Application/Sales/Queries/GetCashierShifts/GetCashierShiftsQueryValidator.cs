using FluentValidation;

namespace Application.Sales.Queries.GetCashierShifts;

public sealed class GetCashierShiftsQueryValidator : AbstractValidator<GetCashierShiftsQuery>
{
    public GetCashierShiftsQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}
