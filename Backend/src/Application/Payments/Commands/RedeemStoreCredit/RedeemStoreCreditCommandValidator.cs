using FluentValidation;

namespace Application.Payments.Commands.RedeemStoreCredit;

public sealed class RedeemStoreCreditCommandValidator : AbstractValidator<RedeemStoreCreditCommand>
{
    public RedeemStoreCreditCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
