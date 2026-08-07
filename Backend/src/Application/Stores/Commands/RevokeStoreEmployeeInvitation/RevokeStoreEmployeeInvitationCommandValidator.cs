using FluentValidation;

namespace Application.Stores.Commands.RevokeStoreEmployeeInvitation;

public sealed class RevokeStoreEmployeeInvitationCommandValidator : AbstractValidator<RevokeStoreEmployeeInvitationCommand>
{
    public RevokeStoreEmployeeInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
