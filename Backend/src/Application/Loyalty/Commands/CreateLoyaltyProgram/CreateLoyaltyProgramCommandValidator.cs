using FluentValidation;

namespace Application.Loyalty.Commands.CreateLoyaltyProgram;

public sealed class CreateLoyaltyProgramCommandValidator : AbstractValidator<CreateLoyaltyProgramCommand>
{
    public CreateLoyaltyProgramCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.PointsPerCurrencyUnit).GreaterThan(0);
        RuleFor(x => x.RedemptionRate).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
