using FluentValidation;

namespace Application.Loyalty.Commands.EnrollCustomerInLoyalty;

public sealed class EnrollCustomerInLoyaltyCommandValidator : AbstractValidator<EnrollCustomerInLoyaltyCommand>
{
    public EnrollCustomerInLoyaltyCommandValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.LoyaltyProgramId).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
