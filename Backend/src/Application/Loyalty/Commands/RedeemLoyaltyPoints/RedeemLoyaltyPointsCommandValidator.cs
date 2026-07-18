using FluentValidation;

namespace Application.Loyalty.Commands.RedeemLoyaltyPoints;

public sealed class RedeemLoyaltyPointsCommandValidator : AbstractValidator<RedeemLoyaltyPointsCommand>
{
    public RedeemLoyaltyPointsCommandValidator()
    {
        RuleFor(x => x.LoyaltyAccountId).GreaterThan(0);
        RuleFor(x => x.Points).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
