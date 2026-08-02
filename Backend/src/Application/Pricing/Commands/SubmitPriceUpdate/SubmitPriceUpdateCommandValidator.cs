using Application.Common;
using FluentValidation;

namespace Application.Pricing.Commands.SubmitPriceUpdate;

public sealed class SubmitPriceUpdateCommandValidator : AbstractValidator<SubmitPriceUpdateCommand>
{
    public SubmitPriceUpdateCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
        // Upper bound closes VAL-01: an unbounded price feeds directly into
        // CompareStoresForShoppingListQueryHandler's summed total with no sanity check.
        RuleFor(x => x.Price).GreaterThan(0).LessThanOrEqualTo(1_000_000);
        RuleFor(x => x.Currency).NotEmpty().Must(SupportedCurrencies.IsSupported).WithMessage("Unsupported currency.");
    }
}
