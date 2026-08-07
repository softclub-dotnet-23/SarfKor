using FluentValidation;

namespace Application.Stores.Commands.ResendStoreEmployeeInvitation;

public sealed class ResendStoreEmployeeInvitationCommandValidator : AbstractValidator<ResendStoreEmployeeInvitationCommand>
{
    public ResendStoreEmployeeInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
