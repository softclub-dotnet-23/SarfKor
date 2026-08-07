using FluentValidation;

namespace Application.Stores.Commands.AcceptStoreEmployeeInvitation;

public sealed class AcceptStoreEmployeeInvitationCommandValidator : AbstractValidator<AcceptStoreEmployeeInvitationCommand>
{
    public AcceptStoreEmployeeInvitationCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        // Whether a password is actually required depends on whether the invitee already has an
        // account — DB state the validator can't see. When one IS supplied it still has to meet the
        // real password policy; when it's absent, the handler decides if that's actually allowed.
        RuleFor(x => x.Password).MinimumLength(8).When(x => !string.IsNullOrEmpty(x.Password));
    }
}
