using FluentValidation;

namespace Application.Identity.Queries.GetUserProfile;

public sealed class GetUserProfileQueryValidator : AbstractValidator<GetUserProfileQuery>
{
    public GetUserProfileQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
