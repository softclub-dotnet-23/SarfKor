using FluentValidation;

namespace Application.Stores.Queries.GetStoreEmployeeInvitationByToken;

public sealed class GetStoreEmployeeInvitationByTokenQueryValidator : AbstractValidator<GetStoreEmployeeInvitationByTokenQuery>
{
    public GetStoreEmployeeInvitationByTokenQueryValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(128);
    }
}
