using FluentValidation;

namespace Application.Stores.Commands.AcceptStoreEmployeeInvitation;

public sealed class AcceptStoreEmployeeInvitationCommandValidator : AbstractValidator<AcceptStoreEmployeeInvitationCommand>
{
    public AcceptStoreEmployeeInvitationCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
