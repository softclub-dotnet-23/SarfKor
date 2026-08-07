using FluentValidation;

namespace Application.Reputation.Commands.AdjustTrustScore;

public sealed class AdjustTrustScoreCommandValidator : AbstractValidator<AdjustTrustScoreCommand>
{
    public AdjustTrustScoreCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.PerformedByAdminUserId).NotEmpty();
        RuleFor(x => x.Delta).NotEqual(0);
    }
}
