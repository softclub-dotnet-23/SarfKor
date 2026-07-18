using FluentValidation;

namespace Application.Engagement.Queries.GetFavorites;

public sealed class GetFavoritesQueryValidator : AbstractValidator<GetFavoritesQuery>
{
    public GetFavoritesQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
