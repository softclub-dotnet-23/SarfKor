using FluentValidation;

namespace Application.Sales.Commands.CloseCashierShift;

public sealed class CloseCashierShiftCommandValidator : AbstractValidator<CloseCashierShiftCommand>
{
    public CloseCashierShiftCommandValidator()
    {
        RuleFor(x => x.CashierShiftId).GreaterThan(0);
        RuleFor(x => x.ClosingCash).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
