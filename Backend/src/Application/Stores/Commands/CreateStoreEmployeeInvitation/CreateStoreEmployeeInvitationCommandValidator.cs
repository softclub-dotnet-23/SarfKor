using FluentValidation;

namespace Application.Stores.Commands.CreateStoreEmployeeInvitation;

public sealed class CreateStoreEmployeeInvitationCommandValidator : AbstractValidator<CreateStoreEmployeeInvitationCommand>
{
    public CreateStoreEmployeeInvitationCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
