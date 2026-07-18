using FluentValidation;

namespace Application.Payments.Queries.GetGiftCardBalance;

public sealed class GetGiftCardBalanceQueryValidator : AbstractValidator<GetGiftCardBalanceQuery>
{
    public GetGiftCardBalanceQueryValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
    }
}
