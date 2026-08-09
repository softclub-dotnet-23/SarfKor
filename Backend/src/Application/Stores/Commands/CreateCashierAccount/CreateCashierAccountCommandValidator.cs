using Application.Common;
using FluentValidation;

namespace Application.Stores.Commands.CreateCashierAccount;

public sealed class CreateCashierAccountCommandValidator : AbstractValidator<CreateCashierAccountCommand>
{
    public CreateCashierAccountCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.PhoneNumber).NotEmpty().Must(TajikPhoneNumber.IsValid)
            .WithMessage("Введите номер телефона в формате +992 XX XXX XX XX.");
        RuleFor(x => x.PerformedByUserId).NotEmpty();

        // Both set or both unset — a shift with only a start or only an end isn't a valid range.
        RuleFor(x => x.ScheduleEnd).NotNull().When(x => x.ScheduleStart is not null)
            .WithMessage("Укажите время окончания смены.");
        RuleFor(x => x.ScheduleStart).NotNull().When(x => x.ScheduleEnd is not null)
            .WithMessage("Укажите время начала смены.");
    }
}
