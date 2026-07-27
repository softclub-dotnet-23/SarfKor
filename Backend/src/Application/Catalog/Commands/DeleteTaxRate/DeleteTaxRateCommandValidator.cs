using FluentValidation;

namespace Application.Catalog.Commands.DeleteTaxRate;

public sealed class DeleteTaxRateCommandValidator : AbstractValidator<DeleteTaxRateCommand>
{
    public DeleteTaxRateCommandValidator()
    {
        RuleFor(x => x.TaxRateId).GreaterThan(0);
    }
}
