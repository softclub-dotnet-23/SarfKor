using FluentValidation;

namespace Application.Reputation.Queries.GetTrustScoreHistory;

public sealed class GetTrustScoreHistoryQueryValidator : AbstractValidator<GetTrustScoreHistoryQuery>
{
    public GetTrustScoreHistoryQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
