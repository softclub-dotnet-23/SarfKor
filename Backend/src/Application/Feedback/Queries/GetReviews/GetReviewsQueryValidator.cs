using FluentValidation;

namespace Application.Feedback.Queries.GetReviews;

public sealed class GetReviewsQueryValidator : AbstractValidator<GetReviewsQuery>
{
    public GetReviewsQueryValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
    }
}
