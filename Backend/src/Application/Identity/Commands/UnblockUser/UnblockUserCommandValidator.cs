using FluentValidation;

namespace Application.Identity.Commands.UnblockUser;

public sealed class UnblockUserCommandValidator : AbstractValidator<UnblockUserCommand>
{
    public UnblockUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.PerformedByAdminUserId).NotEmpty();
    }
}
