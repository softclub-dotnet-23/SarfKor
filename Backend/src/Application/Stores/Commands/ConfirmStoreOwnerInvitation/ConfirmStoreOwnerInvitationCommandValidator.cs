using FluentValidation;

namespace Application.Stores.Commands.ConfirmStoreOwnerInvitation;

public sealed class ConfirmStoreOwnerInvitationCommandValidator : AbstractValidator<ConfirmStoreOwnerInvitationCommand>
{
    public ConfirmStoreOwnerInvitationCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Matches("^[0-9]{6}$");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
