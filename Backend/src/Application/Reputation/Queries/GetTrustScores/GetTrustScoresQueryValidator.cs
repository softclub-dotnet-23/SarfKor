using FluentValidation;

namespace Application.Reputation.Queries.GetTrustScores;

public sealed class GetTrustScoresQueryValidator : AbstractValidator<GetTrustScoresQuery>
{
    public GetTrustScoresQueryValidator()
    {
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take).InclusiveBetween(1, 200);
    }
}
