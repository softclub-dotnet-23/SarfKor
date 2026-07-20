using FluentValidation;

namespace Application.Identity.Queries.GetUserConsents;

public sealed class GetUserConsentsQueryValidator : AbstractValidator<GetUserConsentsQuery>
{
    public GetUserConsentsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
