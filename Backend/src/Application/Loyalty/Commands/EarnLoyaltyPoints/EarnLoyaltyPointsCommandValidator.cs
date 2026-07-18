using FluentValidation;

namespace Application.Loyalty.Commands.EarnLoyaltyPoints;

public sealed class EarnLoyaltyPointsCommandValidator : AbstractValidator<EarnLoyaltyPointsCommand>
{
    public EarnLoyaltyPointsCommandValidator()
    {
        RuleFor(x => x.LoyaltyAccountId).GreaterThan(0);
        RuleFor(x => x.Points).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
