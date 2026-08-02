using Application.Common;
using FluentValidation;

namespace Application.Sales.Commands.RecordCommission;

public sealed class RecordCommissionCommandValidator : AbstractValidator<RecordCommissionCommand>
{
    public RecordCommissionCommandValidator()
    {
        RuleFor(x => x.SaleTransactionId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Must(SupportedCurrencies.IsSupported).WithMessage("Unsupported currency.");
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}
