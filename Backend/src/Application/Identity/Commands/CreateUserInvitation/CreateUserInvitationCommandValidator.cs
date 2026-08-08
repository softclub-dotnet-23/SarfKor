using FluentValidation;

namespace Application.Identity.Commands.CreateUserInvitation;

public sealed class CreateUserInvitationCommandValidator : AbstractValidator<CreateUserInvitationCommand>
{
    private static readonly string[] AllowedRoles = ["User", "StorePartner", "Admin"];

    public CreateUserInvitationCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.InvitedRole).NotEmpty().Must(role => AllowedRoles.Contains(role))
            .WithMessage("InvitedRole must be one of: User, StorePartner, Admin.");
        RuleFor(x => x.PerformedByUserId).NotEmpty();

        RuleFor(x => x.StoreId).NotNull().GreaterThan(0)
            .When(x => x.InvitedRole == "StorePartner")
            .WithMessage("StoreId is required when InvitedRole is StorePartner.");
        RuleFor(x => x.StoreId).Null()
            .When(x => x.InvitedRole != "StorePartner")
            .WithMessage("StoreId must not be set unless InvitedRole is StorePartner.");
    }
}
