using Application.Common;
using FluentValidation;

namespace Application.Sales.Commands.OpenCashierShift;

public sealed class OpenCashierShiftCommandValidator : AbstractValidator<OpenCashierShiftCommand>
{
    public OpenCashierShiftCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.OpeningCash).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Must(SupportedCurrencies.IsSupported).WithMessage("Unsupported currency.");
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
