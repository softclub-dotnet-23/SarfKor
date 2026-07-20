using FluentValidation;

namespace Application.Identity.Queries.GetSecurityEvents;

public sealed class GetSecurityEventsQueryValidator : AbstractValidator<GetSecurityEventsQuery>
{
    public GetSecurityEventsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
