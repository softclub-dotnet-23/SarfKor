using FluentValidation;

namespace Application.Identity.Commands.InviteAdmin;

public sealed class InviteAdminCommandValidator : AbstractValidator<InviteAdminCommand>
{
    public InviteAdminCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.InvitedByAdminUserId).NotEmpty();
    }
}
