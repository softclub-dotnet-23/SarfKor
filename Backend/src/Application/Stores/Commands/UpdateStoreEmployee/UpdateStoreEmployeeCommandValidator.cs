using Application.Common;
using FluentValidation;

namespace Application.Stores.Commands.UpdateStoreEmployee;

public sealed class UpdateStoreEmployeeCommandValidator : AbstractValidator<UpdateStoreEmployeeCommand>
{
    public UpdateStoreEmployeeCommandValidator()
    {
        RuleFor(x => x.StoreEmployeeId).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100).When(x => x.FirstName is not null);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100).When(x => x.LastName is not null);
        RuleFor(x => x.PhoneNumber).Must(TajikPhoneNumber.IsValid)
            .WithMessage("Введите номер телефона в формате +992 XX XXX XX XX.")
            .When(x => x.PhoneNumber is not null);

        RuleFor(x => x.MonthlySalaryAmount).GreaterThanOrEqualTo(0).When(x => x.MonthlySalaryAmount.HasValue);
        RuleFor(x => x.MonthlySalaryCurrency)
            .NotEmpty()
            .Must(SupportedCurrencies.IsSupported).WithMessage("Unsupported currency.")
            .When(x => x.MonthlySalaryAmount.HasValue);
        RuleFor(x => x.MonthlySalaryCurrency)
            .Null().WithMessage("MonthlySalaryAmount and MonthlySalaryCurrency must be set together.")
            .When(x => !x.MonthlySalaryAmount.HasValue);

        // No End > Start rule — an overnight shift (e.g. 22:00 -> 06:00) is legitimate.
        RuleFor(x => x.ScheduleEnd)
            .NotNull().WithMessage("ScheduleStart and ScheduleEnd must be set together.")
            .When(x => x.ScheduleStart.HasValue);
        RuleFor(x => x.ScheduleStart)
            .NotNull().WithMessage("ScheduleStart and ScheduleEnd must be set together.")
            .When(x => x.ScheduleEnd.HasValue);
    }
}
