using FluentValidation;

namespace Application.Identity.Queries.GetUserDetail;

public sealed class GetUserDetailQueryValidator : AbstractValidator<GetUserDetailQuery>
{
    public GetUserDetailQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
