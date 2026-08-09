using FluentValidation;

namespace Application.Stores.Commands.CreateCashierAccount;

public sealed class CreateCashierAccountCommandValidator : AbstractValidator<CreateCashierAccountCommand>
{
    public CreateCashierAccountCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
