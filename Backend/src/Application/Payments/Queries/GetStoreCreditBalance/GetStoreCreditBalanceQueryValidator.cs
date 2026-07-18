using FluentValidation;

namespace Application.Payments.Queries.GetStoreCreditBalance;

public sealed class GetStoreCreditBalanceQueryValidator : AbstractValidator<GetStoreCreditBalanceQuery>
{
    public GetStoreCreditBalanceQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}
